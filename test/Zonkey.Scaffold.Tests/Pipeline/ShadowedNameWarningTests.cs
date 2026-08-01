using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Scaffold.Commands;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;

/// <summary>
/// The two collisions with names the tool does not declare but does depend on.
/// </summary>
/// <remarks>
/// Both were carried as open decisions on <see cref="EmittedSurface"/> for three fix waves, and
/// both are now warnings rather than refusals: the caller gets the files and is told, in the same
/// breath, what will not compile and which override key fixes it. The tests therefore assert the
/// warning <em>and</em> that generation still succeeded and still wrote the file — a refusal
/// dressed as a warning would pass half of each of these.
/// </remarks>
public class ShadowedNameWarningTests : IAsyncLifetime
{
    private string _dir = "";

    public ValueTask InitializeAsync()
    {
        _dir = Directory.CreateTempSubdirectory("zshadow").FullName;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
        return ValueTask.CompletedTask;
    }

    // ---- fixtures ---------------------------------------------------------------

    /// <summary>A column named after a public <c>DataClass</c> member (CS0108).</summary>
    private const string ShadowingColumnDdl = """
        CREATE TABLE widgets (
            WidgetId     INTEGER PRIMARY KEY AUTOINCREMENT,
            DataRowState TEXT,
            Label        TEXT
        );
        """;

    /// <summary>
    /// A table whose class name is the base type the emitted class derives from: the class ends up
    /// deriving from itself, which is a hard error (CS0146).
    /// </summary>
    private const string ShadowingBaseTypeDdl = """
        CREATE TABLE data_classes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT);
        """;

    /// <summary>
    /// A table whose class name is a type the emitted source merely <em>references</em> — here the
    /// CLR type of a column elsewhere in the same run. Nothing derives from it, so the generated
    /// code compiles; the reference just silently resolves to the generated class instead of
    /// <c>System.Guid</c>. That is the worse outcome of the two and the message must not claim a
    /// build failure that will not happen.
    /// </summary>
    private const string ShadowingReferencedTypeDdl = """
        CREATE TABLE guids (GuidId INTEGER PRIMARY KEY AUTOINCREMENT, Value GUID);
        """;

    /// <summary>
    /// An ordinary schema. Neither warning may fire here: a false positive on this shape would
    /// mean a warning on essentially every real database, which is worse than no warning at all.
    /// </summary>
    private const string ConventionalDdl = """
        CREATE TABLE customers (
            CustomerId   INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerCode TEXT NOT NULL,
            Name         TEXT NOT NULL,
            CreatedAt    DATETIME,
            Status       TEXT
        );
        CREATE TABLE orders (
            OrderId    INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerId INTEGER NOT NULL,
            OrderDate  DATETIME NOT NULL,
            Total      NUMERIC(10,2)
        );
        CREATE TABLE order_items (
            OrderItemId INTEGER PRIMARY KEY AUTOINCREMENT,
            OrderId     INTEGER NOT NULL,
            Quantity    INTEGER NOT NULL
        );
        -- Key-less, so it is emitted read-only: the shape whose base type is `object` rather
        -- than DataClass, and therefore the shape a wrong base-type choice would misjudge.
        CREATE TABLE audit_log (Message TEXT NOT NULL, LoggedAt DATETIME);
        """;

    private async Task<string> Database(string name, string ddl)
    {
        string conn = $"Data Source={Path.Combine(_dir, name + ".db")}";

        await using (var cnxn = new SqliteConnection(conn))
        {
            await cnxn.OpenAsync(TestContext.Current.CancellationToken);
            await using var cmd = cnxn.CreateCommand();
            cmd.CommandText = ddl;
            await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        SqliteConnection.ClearAllPools();
        return conn;
    }

    private ScaffoldOptions Options(string conn, string outDir)
    {
        var o = new ScaffoldOptions
        {
            Provider = "sqlite",
            ConnectionString = conn,
            Namespace = "Zoo.Data",
            Wrapper = new WrapperOptions { ClassName = "ZooDatabase", ConnectionName = "Zoo" }
        };

        o.Output.Entities = outDir;
        return o;
    }

    private Task<ScaffoldPlan> Build(ScaffoldOptions o) => ScaffoldPipeline.Build(
        o, new ProjectCapabilities(), OutputLayout.Resolve(o, _dir),
        TestContext.Current.CancellationToken);

    // ---- CS0108: a property hides an inherited DataClass member -------------------

    [Fact]
    public async Task Column_named_after_a_DataClass_member_warns_and_still_generates()
    {
        string conn = await Database("shadowcol", ShadowingColumnDdl);
        ScaffoldOptions o = Options(conn, "Cs0108");

        ScaffoldPlan plan = await Build(o);

        ScaffoldWarning w = Assert.Single(
            plan.Warnings, x => x.Code == WarningCode.HidesBaseMember);

        Assert.Equal(WarningLevel.Warning, w.Level);
        Assert.Equal("main.widgets", w.Table);
        Assert.Equal("DataRowState", w.Column);

        // Names the table, the column, what breaks, and the key that fixes it.
        Assert.Contains("main.widgets", w.Message);
        Assert.Contains("DataRowState", w.Message);
        Assert.Contains("CS0108", w.Message);
        Assert.Contains(
            "overrides.tables.widgets.columns.DataRowState.property", w.Message);

        // A warning, not a refusal: the file is written and the run exits 0.
        var sw = new StringWriter();
        int exit = await GenerateCommand.Run(
            Options(conn, "Cs0108"), dryRun: false, json: false, workingDirectory: _dir,
            stdout: sw, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(_dir, "Cs0108", "Widget.g.cs")));
    }

    /// <summary>
    /// A read-only entity has no base type at all, so it can only collide with
    /// <see cref="object"/>'s members — the same column that shadows <c>DataClass.DataRowState</c>
    /// on a savable class is unremarkable here, and a column named <c>ToString</c> is not.
    /// </summary>
    [Fact]
    public async Task Read_only_entities_are_checked_against_object_and_not_DataClass()
    {
        string conn = await Database("readonlyshadow", """
            CREATE TABLE audit_log (DataRowState TEXT, ToString TEXT);
            """);

        ScaffoldPlan plan = await Build(Options(conn, "ReadOnly"));

        Assert.True(plan.Entities.Single().IsReadOnly);

        ScaffoldWarning w = Assert.Single(
            plan.Warnings, x => x.Code == WarningCode.HidesBaseMember);

        Assert.Equal("ToString", w.Column);
    }

    // ---- a class named after a type the emitted source depends on -------------------
    //
    // Two rules, because the two halves of the old single rule have opposite consequences and the
    // one message could only be true of one of them. A class named after its own base type does
    // not compile; a class named after a merely-referenced type compiles perfectly and quietly
    // means something else. Promising a build failure that does not arrive is worse than saying
    // nothing: the caller builds clean, concludes the warning was spurious, and discounts the next
    // one too.

    [Fact]
    public async Task Class_named_after_its_base_type_warns_that_it_will_not_compile()
    {
        string conn = await Database("shadowbase", ShadowingBaseTypeDdl);
        ScaffoldOptions o = Options(conn, "Cs0146");

        ScaffoldPlan plan = await Build(o);

        Assert.Equal("DataClass", plan.Entities.Single().ClassName);

        ScaffoldWarning w = Assert.Single(
            plan.Warnings, x => x.Code == WarningCode.ShadowsBaseType);

        Assert.Equal(WarningLevel.Warning, w.Level);
        Assert.Equal("main.data_classes", w.Table);

        Assert.Contains("main.data_classes", w.Message);
        Assert.Contains("DataClass", w.Message);
        Assert.Contains("CS0146", w.Message);
        Assert.Contains("will not compile", w.Message);
        Assert.Contains("overrides.tables.data_classes.className", w.Message);

        // The milder rule must not also fire: one fault, one warning.
        Assert.DoesNotContain(plan.Warnings, x => x.Code == WarningCode.ShadowsReferencedType);

        var sw = new StringWriter();
        int exit = await GenerateCommand.Run(
            Options(conn, "Cs0146"), dryRun: false, json: false, workingDirectory: _dir,
            stdout: sw, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(_dir, "Cs0146", "DataClass.g.cs")));
    }

    [Fact]
    public async Task Class_named_after_a_referenced_type_warns_about_a_silent_rebind()
    {
        string conn = await Database("shadowref", ShadowingReferencedTypeDdl);
        ScaffoldOptions o = Options(conn, "Rebind");

        ScaffoldPlan plan = await Build(o);

        Assert.Equal("Guid", plan.Entities.Single().ClassName);

        ScaffoldWarning w = Assert.Single(
            plan.Warnings, x => x.Code == WarningCode.ShadowsReferencedType);

        Assert.Equal(WarningLevel.Warning, w.Level);
        Assert.Equal("main.guids", w.Table);

        Assert.Contains("main.guids", w.Message);
        Assert.Contains("Guid", w.Message);
        Assert.Contains("overrides.tables.guids.className", w.Message);

        // The point of the split: this code compiles. Claiming otherwise is the defect being
        // fixed, so the message may not say so and may not cite the hard-error diagnostic.
        Assert.DoesNotContain("will not compile", w.Message);
        Assert.DoesNotContain("CS0146", w.Message);
        Assert.Contains("compiles", w.Message);

        Assert.DoesNotContain(plan.Warnings, x => x.Code == WarningCode.ShadowsBaseType);

        var sw = new StringWriter();
        int exit = await GenerateCommand.Run(
            Options(conn, "Rebind"), dryRun: false, json: false, workingDirectory: _dir,
            stdout: sw, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(_dir, "Rebind", "Guid.g.cs")));
    }

    // ---- reporting ----------------------------------------------------------------

    [Fact]
    public async Task Every_new_warning_reaches_inspect_json_and_the_generate_summary()
    {
        string conn = await Database("both", string.Join("\n",
            ShadowingColumnDdl, ShadowingBaseTypeDdl, ShadowingReferencedTypeDdl));

        var inspect = new StringWriter();
        Assert.Equal(0, await InspectCommand.Run(
            Options(conn, "Both"), json: true, workingDirectory: _dir, stdout: inspect,
            ct: TestContext.Current.CancellationToken));

        string json = inspect.ToString();
        Assert.Contains(WarningCode.HidesBaseMember, json);
        Assert.Contains(WarningCode.ShadowsBaseType, json);
        Assert.Contains(WarningCode.ShadowsReferencedType, json);

        // The summary counts everything that is not `info`, so both must be in the number a
        // caller reads — the whole point of choosing `warning` over `info`.
        ScaffoldPlan plan = await Build(Options(conn, "BothPlan"));
        int expected = plan.Warnings.Count(x => x.Level != WarningLevel.Info);

        var generate = new StringWriter();
        Assert.Equal(0, await GenerateCommand.Run(
            Options(conn, "Both"), dryRun: false, json: false, workingDirectory: _dir,
            stdout: generate, ct: TestContext.Current.CancellationToken));

        Assert.Contains($"{expected} warning(s)", generate.ToString());
        Assert.True(expected >= 3, "All three new warnings must be inside the counted set.");
    }

    // ---- the false-positive guard --------------------------------------------------

    [Fact]
    public async Task A_conventional_schema_raises_neither_warning()
    {
        string conn = await Database("conventional", ConventionalDdl);

        ScaffoldPlan plan = await Build(Options(conn, "Conventional"));

        Assert.DoesNotContain(plan.Warnings, w => w.Code == WarningCode.HidesBaseMember);
        Assert.DoesNotContain(plan.Warnings, w => w.Code == WarningCode.ShadowsBaseType);
        Assert.DoesNotContain(plan.Warnings, w => w.Code == WarningCode.ShadowsReferencedType);

        // The fixture is only a guard if it really covered the shapes these rules run over:
        // a savable entity, a read-only one, and the wrapper.
        Assert.Contains(plan.Emitted, u => u.Entity?.IsReadOnly == false);
        Assert.Contains(plan.Emitted, u => u.Entity?.IsReadOnly == true);
        Assert.Contains(plan.Emitted, u => u.IsWrapper);
    }

    // ---- the reflection itself ------------------------------------------------------

    /// <summary>
    /// The member set is reflected off <c>DataClass</c> rather than transcribed, so nothing in the
    /// suite would notice if the reflection silently returned nothing (wrong binding flags, an
    /// accessibility filter that excludes everything) — every test above would still pass except
    /// the one that expects a warning, and that one names a single member. This pins the shape of
    /// the set: the members that must be in it, the ones that must not, and a size that would
    /// catch "everything" as well as "nothing".
    /// </summary>
    [Fact]
    public void The_inherited_member_set_is_reflected_and_neither_empty_nor_absurd()
    {
        IReadOnlySet<string> dataClass = EmittedSurface.InheritedMemberNames(
            typeof(Zonkey.ObjectModel.DataClass));

        // Declared by DataClass, public or protected, and therefore hideable.
        Assert.Contains("DataRowState", dataClass);
        Assert.Contains("OriginalValues", dataClass);
        Assert.Contains("CommitValues", dataClass);
        Assert.Contains("SetFieldValue", dataClass);
        Assert.Contains("GetKeyFields", dataClass);

        // Inherited from object, which an emitted entity also derives from.
        Assert.Contains("ToString", dataClass);
        Assert.Contains("Equals", dataClass);
        Assert.Contains("GetHashCode", dataClass);
        Assert.Contains("GetType", dataClass);

        // Private base fields are not inherited members, so hiding one is not a diagnostic —
        // and `_originalValues` is exactly the name a column called `OriginalValues` would give
        // its backing field, so including it would be a false positive by construction.
        Assert.DoesNotContain("_originalValues", dataClass);
        Assert.DoesNotContain("_dataRowState", dataClass);

        // Constructors are not members of the declaration space, and property accessors are not
        // identifiers anyone writes.
        Assert.DoesNotContain(".ctor", dataClass);
        Assert.DoesNotContain("get_DataRowState", dataClass);

        // Small enough to be a real filter, large enough to be a real reflection.
        Assert.InRange(dataClass.Count, 9, 40);

        // A read-only entity has no base type; the rule still has object to check against, and
        // that set must be a strict subset of DataClass's.
        IReadOnlySet<string> obj = EmittedSurface.InheritedMemberNames(typeof(object));
        Assert.Contains("ToString", obj);
        Assert.DoesNotContain("DataRowState", obj);
        Assert.True(obj.IsProperSubsetOf(dataClass));
    }
}
