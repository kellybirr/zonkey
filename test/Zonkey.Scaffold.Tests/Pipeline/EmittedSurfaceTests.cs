using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Scaffold.Commands;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;

/// <summary>
/// The structural guard on collision detection.
/// </summary>
/// <remarks>
/// Three waves of fixes to this area each added a check for the identifier family that had just
/// been reported and each missed a sibling, because the checks were written from a hand-made list
/// of what the emitters were believed to declare. These tests do not name a family at all. They
/// take the surface the pipeline computed, ask the emitters what they actually declare (by parsing
/// their output) and ask the filesystem what was actually written, and require the three to agree
/// exactly.
/// <para>
/// So an identifier family added to an emitter later — a nested type, a constant, a second
/// property per column — fails these tests on the day it is added, whether or not anyone remembers
/// that <c>EmittedSurface</c> exists. That is the property the three previous waves lacked.
/// </para>
/// </remarks>
public class EmittedSurfaceTests : IAsyncLifetime
{
    private string _dir = "";
    private string _conn = "";

    /// <summary>
    /// Deliberately covers all three emit shapes in one run, because the surface has to be right
    /// per class and not per run: <c>things</c> has a property named <c>field</c> and so loses
    /// <c>field</c>-keyword mode (and gains backing fields) while <c>plain</c>, same run and same
    /// options, keeps it; <c>notes</c> has no primary key and is emitted read-only, which declares
    /// no backing fields either and no wrapper property. It also carries a keyword column, so the
    /// <c>@</c> escape has to survive the comparison, and a column that must be renamed.
    /// </summary>
    private const string Ddl = """
        CREATE TABLE things (
            Id      INTEGER PRIMARY KEY AUTOINCREMENT,
            "field" TEXT,
            Label   TEXT,
            "class" TEXT
        );
        CREATE TABLE plain (
            Id         INTEGER PRIMARY KEY,
            Name       TEXT,
            -- The two CLR types the mapper names rather than spells with a keyword. They reach
            -- emitted source as bare simple names, so they belong to the set of names a generated
            -- class must not be called; without a column of each, the shadow-set test below could
            -- not see them.
            CreatedAt  DATETIME,
            ExternalId GUID
        );
        CREATE TABLE notes (Body TEXT NOT NULL);
        """;

    public async ValueTask InitializeAsync()
    {
        _dir = Directory.CreateTempSubdirectory("zsurface").FullName;
        _conn = $"Data Source={Path.Combine(_dir, "surface.db")}";

        await using (var cnxn = new SqliteConnection(_conn))
        {
            await cnxn.OpenAsync();
            await using var cmd = cnxn.CreateCommand();
            cmd.CommandText = Ddl;
            await cmd.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
        return ValueTask.CompletedTask;
    }

    private ScaffoldOptions Options()
    {
        var o = new ScaffoldOptions
        {
            Provider = "sqlite",
            ConnectionString = _conn,
            Namespace = "Zoo.Data",
            Wrapper = new WrapperOptions { ClassName = "ZooDatabase", ConnectionName = "Zoo" }
        };

        o.Output.Entities = "Entities";
        // A wrapper directory of its own, so the path assertions cover a split layout rather than
        // the case where every file happens to land in one directory anyway.
        o.Output.Wrapper = "Wrapper";
        o.Emit.FieldKeyword = "true";
        o.Emit.NullableRefs = "true";

        // PascalCasing would give `Field`, which is an ordinary property name; the emitter only
        // loses `field` mode for a member literally named `field`.
        o.Overrides.Tables["things"] = new TableOverride
        {
            Columns =
            {
                ["field"] = new ColumnOverride { Property = "field" },
                // Escaped to `@class`, so the comparison has to be against the identifier as the
                // source spells it — the surface carries the escape and so must the parse.
                ["class"] = new ColumnOverride { Property = "class" }
            }
        };

        return o;
    }

    private async Task<(ScaffoldPlan Plan, OutputLayout Layout, ScaffoldOptions Options)> Build()
    {
        ScaffoldOptions o = Options();
        OutputLayout layout = OutputLayout.Resolve(o, _dir);

        ScaffoldPlan plan = await ScaffoldPipeline.Build(
            o, new ProjectCapabilities(), layout, TestContext.Current.CancellationToken);

        return (plan, layout, o);
    }

    /// <summary>
    /// Every identifier the emitters declare is in the surface the collision check ran over, and
    /// every identifier in that surface is one the emitters declare — in both directions, so the
    /// test fails both for a family that is emitted without being checked (the defect) and for one
    /// that is checked without being emitted (which would refuse working configurations).
    /// </summary>
    [Fact]
    public async Task Every_identifier_the_emitters_declare_is_in_the_checked_surface()
    {
        (ScaffoldPlan plan, _, ScaffoldOptions o) = await Build();

        var emitOptions = new EntityEmitOptions
        {
            Namespace = plan.Namespace,
            PartialClasses = o.Emit.PartialClasses,
            VirtualProperties = o.Emit.VirtualProperties,
            FieldKeyword = plan.FieldKeyword,
            PrivateFieldsAtTop = o.Emit.PrivateFieldsAtTop,
            NullableRefs = plan.NullableRefs
        };

        Assert.NotEmpty(plan.Emitted);

        foreach (EmittedType unit in plan.Emitted)
        {
            string source = unit.IsWrapper
                ? new CSharpWrapperEmitter().Emit(plan.Wrapper)
                : new CSharpEntityEmitter().Emit(unit.Entity!, emitOptions);

            (string typeName, List<string> members) = Declarations(source);

            Assert.Equal(unit.ClassName, typeName);

            Assert.Equal(
                members.Order(StringComparer.Ordinal),
                unit.Members.Select(m => m.Name).Order(StringComparer.Ordinal));
        }

        // The fixture is only meaningful if it really did produce all three emit shapes — a class
        // with backing fields, one without, and a read-only one — so pin that here rather than
        // trust the DDL to keep meaning what it means.
        Assert.Contains(plan.Emitted, u => u.Members.Any(m => m.Kind == DeclaredKind.BackingField));
        Assert.Contains(plan.Emitted, u =>
            u is { IsWrapper: false, Entity.IsReadOnly: false } &&
            u.Members.All(m => m.Kind != DeclaredKind.BackingField));
        Assert.Contains(plan.Emitted, u => u.Entity?.IsReadOnly == true);
        Assert.Contains(plan.Emitted, u => u.IsWrapper);
    }

    /// <summary>
    /// The path half of the same property: <c>generate</c> writes exactly the files the surface
    /// named, so "would these two entities land in one file?" is asked of the paths that are then
    /// actually used. A second derivation of the output paths in the writer is how a wrapper file
    /// came to land beside an entity file that the check had excluded it from meeting.
    /// </summary>
    [Fact]
    public async Task Generate_writes_exactly_the_files_the_surface_named()
    {
        (ScaffoldPlan plan, _, ScaffoldOptions o) = await Build();

        var sw = new StringWriter();
        int exit = await GenerateCommand.Run(
            Options(), dryRun: false, json: false, workingDirectory: _dir, stdout: sw,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);

        IEnumerable<string> written = Directory
            .EnumerateFiles(_dir, "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal);

        Assert.Equal(plan.Emitted.Select(u => u.FilePath).Order(StringComparer.Ordinal), written);

        // The assertion above is only interesting if the layout is not degenerate: the fixture
        // gives the wrapper a directory of its own, so a writer that rebuilt the paths from
        // `output.entities` would land it somewhere the surface never named.
        Assert.NotEqual(
            Path.GetDirectoryName(plan.Emitted.Single(u => u.IsWrapper).FilePath),
            Path.GetDirectoryName(plan.Emitted.First(u => !u.IsWrapper).FilePath));
    }

    /// <summary>
    /// The other half of the same idea, for the other direction of dependency: every type the
    /// emitted source <em>references</em> by simple name is in the set the shadow check protects.
    /// </summary>
    /// <remarks>
    /// A generated class named after one of those names silently rebinds the reference — a class
    /// named <c>DataClass</c> derives from itself (CS0146). The set cannot be derived from the
    /// emitters at run time (they build strings, not syntax), so it is declared beside the emit
    /// code as <c>ReferencedTypeNames</c> and pinned here: this test parses what the emitters
    /// really produced and fails if it names a type the set does not carry.
    /// <para>
    /// That is a test, not a guarantee. It sees only the references the fixture's emit paths
    /// actually take — a type an emitter writes only for, say, a sequence-backed key column, or a
    /// CLR type only another provider's mapper produces, is invisible to it. The fixture is built
    /// to take every branch that exists today (all three emit shapes, the wrapper, and a column of
    /// each named CLR type); a future emitter that references a type on a branch nothing here
    /// exercises will not be caught.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Every_type_the_emitters_reference_is_in_the_shadow_check_set()
    {
        (ScaffoldPlan plan, _, ScaffoldOptions o) = await Build();

        var emitOptions = new EntityEmitOptions
        {
            Namespace = plan.Namespace,
            PartialClasses = o.Emit.PartialClasses,
            VirtualProperties = o.Emit.VirtualProperties,
            FieldKeyword = plan.FieldKeyword,
            PrivateFieldsAtTop = o.Emit.PrivateFieldsAtTop,
            NullableRefs = plan.NullableRefs
        };

        // The run's own classes are the one legitimate exception: the wrapper names every entity
        // type, and those references resolve to the classes this run emits. A class named after
        // another class in the run is a duplicate type name, which EmittedSurface.Check refuses
        // outright — a different fault with a different rule.
        var allowed = new HashSet<string>(
            EmittedSurface.ReferencedTypeNames(plan.Emitted), StringComparer.Ordinal);
        allowed.UnionWith(plan.Emitted.Select(u => u.ClassName));

        var referenced = new HashSet<string>(StringComparer.Ordinal);

        foreach (EmittedType unit in plan.Emitted)
        {
            referenced.UnionWith(References(unit.IsWrapper
                ? new CSharpWrapperEmitter().Emit(plan.Wrapper)
                : new CSharpEntityEmitter().Emit(unit.Entity!, emitOptions)));
        }

        List<string> unprotected = [.. referenced.Except(allowed).Order(StringComparer.Ordinal)];

        Assert.True(unprotected.Count == 0,
            "The emitters reference these type names, but a generated class of the same name " +
            "would not be warned about: " + string.Join(", ", unprotected));

        // Guards the assertion above against a collector that quietly stopped finding anything.
        Assert.Contains("DataClass", referenced);
        Assert.Contains("DatabaseWrapper", referenced);
        Assert.Contains("DbType", referenced);
        Assert.Contains("DateTime", referenced);
        Assert.Contains("Guid", referenced);
    }

    /// <summary>
    /// Every simple type name a generated file references: base types, attribute names (in both
    /// spellings the compiler accepts), declared types, type arguments, and the left-hand side of
    /// a member access, which is how <c>DbType.String</c> and <c>DateTimeKind.Utc</c> reach the
    /// source. Predefined types (<c>string</c>, <c>long</c>) are skipped — they are keywords, and
    /// a class can only be named after one in its escaped form, which is a different identifier.
    /// </summary>
    private static IEnumerable<string> References(string source)
    {
        SyntaxNode root = CSharpSyntaxTree
            .ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))
            .GetRoot();

        var names = new HashSet<string>(StringComparer.Ordinal);

        void AddType(TypeSyntax? type)
        {
            switch (type)
            {
                case null or PredefinedTypeSyntax:
                    break;
                case NullableTypeSyntax n:
                    AddType(n.ElementType);
                    break;
                case ArrayTypeSyntax a:
                    AddType(a.ElementType);
                    break;
                case GenericNameSyntax g:
                    names.Add(g.Identifier.Text);
                    foreach (TypeSyntax arg in g.TypeArgumentList.Arguments) AddType(arg);
                    break;
                case QualifiedNameSyntax q:
                    // Only the leftmost segment can be captured by a type in the file's namespace.
                    AddType(q.Left);
                    break;
                case IdentifierNameSyntax i:
                    names.Add(i.Identifier.Text);
                    break;
            }
        }

        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case SimpleBaseTypeSyntax b:
                    AddType(b.Type);
                    break;
                case AttributeSyntax { Name: IdentifierNameSyntax an }:
                    // `[DataItem]` binds to either `DataItem` or `DataItemAttribute`, so a class
                    // of either name breaks it (and a class of both names is CS1614).
                    names.Add(an.Identifier.Text);
                    names.Add(an.Identifier.Text + "Attribute");
                    break;
                case PropertyDeclarationSyntax p:
                    AddType(p.Type);
                    break;
                case VariableDeclarationSyntax v:
                    AddType(v.Type);
                    break;
                case ParameterSyntax pa:
                    AddType(pa.Type);
                    break;
                case TypeArgumentListSyntax ta:
                    foreach (TypeSyntax arg in ta.Arguments) AddType(arg);
                    break;
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m }:
                    names.Add(m.Identifier.Text);
                    break;
            }
        }

        return names;
    }

    /// <summary>
    /// The one type declared by a generated file, and every identifier it declares apart from its
    /// constructors — which are named after the type by definition, are not members of the
    /// declaration space, and are the only names the emitters produce that the surface does not
    /// carry.
    /// </summary>
    /// <remarks>
    /// The <c>_ =&gt;</c> arm is the point of the whole helper: any member kind an emitter grows
    /// later that this switch does not recognize contributes a name that cannot be in the surface,
    /// so the assertion fails and whoever added it has to say what the collision rule for it is.
    /// </remarks>
    private static (string TypeName, List<string> Members) Declarations(string source)
    {
        SyntaxNode root = CSharpSyntaxTree
            .ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))
            .GetRoot();

        List<BaseTypeDeclarationSyntax> types =
            [.. root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()];

        BaseTypeDeclarationSyntax type = Assert.Single(types);

        var members = new List<string>();

        foreach (MemberDeclarationSyntax member in type.DescendantNodes()
                     .OfType<MemberDeclarationSyntax>())
        {
            switch (member)
            {
                case ConstructorDeclarationSyntax:
                    break;
                case BaseFieldDeclarationSyntax f:
                    members.AddRange(f.Declaration.Variables.Select(v => v.Identifier.Text));
                    break;
                case PropertyDeclarationSyntax p:
                    members.Add(p.Identifier.Text);
                    break;
                case MethodDeclarationSyntax m:
                    members.Add(m.Identifier.Text);
                    break;
                case BaseTypeDeclarationSyntax t:
                    members.Add(t.Identifier.Text);
                    break;
                default:
                    members.Add($"<unrecognized {member.Kind()} declaration>");
                    break;
            }
        }

        return (type.Identifier.Text, members);
    }
}
