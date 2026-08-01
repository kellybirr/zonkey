using System.Text.Json.Serialization;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Naming;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Providers;
using Zonkey.Scaffold.Schema;
using Zonkey.Scaffold.Selection;

namespace Zonkey.Scaffold.Pipeline;

public sealed class ColumnDecision
{
    public string Table { get; set; } = "";
    public string Column { get; set; } = "";
    public string NativeType { get; set; } = "";
    public string ProposedProperty { get; set; } = "";
    public string ProposedClrType { get; set; } = "";
    public string ProposedDbType { get; set; } = "";
    public string Reason { get; set; } = "";
}

public sealed class ScaffoldPlan
{
    public string Provider { get; set; } = "";
    public string ServerVersion { get; set; } = "";

    /// <summary>
    /// The namespace the emitters must use — <c>options.Namespace</c> with every segment escaped.
    /// It lives on the plan rather than being recomputed per emitter because the entities and the
    /// wrapper have to agree: the wrapper names the entity types, so two different spellings of
    /// the namespace do not compile.
    /// </summary>
    public string? Namespace { get; set; }
    public IReadOnlyList<string> Schemas { get; set; } = [];
    public List<EntityModel> Entities { get; set; } = new();
    public WrapperModel Wrapper { get; set; } = new();
    public List<ColumnDecision> Decisions { get; set; } = new();
    public List<ScaffoldWarning> Warnings { get; set; } = new();
    public List<SkipRecord> Skipped { get; set; } = new();

    /// <summary>
    /// Every type this run will emit, with the identifiers it will declare and the absolute path it
    /// will be written to. <c>generate</c> writes from this list, and
    /// <see cref="EmittedSurface.Check"/> has already refused everything in it that could not
    /// compile — so what was checked and what is written are the same objects rather than two
    /// derivations that have to agree.
    /// </summary>
    /// <remarks>
    /// Not serialized: <c>inspect --json</c> is a preview of the mapping decisions, and the
    /// emitted surface would repeat every class and property already in <c>entities</c> alongside
    /// absolute machine-local paths.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyList<EmittedType> Emitted { get; set; } = [];

    /// <summary>
    /// The resolved <c>emit.fieldKeyword</c> / <c>emit.nullableRefs</c> tri-states.
    /// </summary>
    /// <remarks>
    /// Resolved once, here, because three consumers need them: the type mapper (nullability), the
    /// emitted-surface computation (whether a class declares backing fields whose names must be
    /// unique) and <c>GenerateCommand</c> (the emit options). <c>GenerateCommand</c> used to
    /// re-derive both from <c>capabilities</c> — a rule in two places, which is the shape of defect
    /// this file has now been fixed for three times.
    /// </remarks>
    [JsonIgnore]
    public bool FieldKeyword { get; set; }

    /// <inheritdoc cref="FieldKeyword"/>
    [JsonIgnore]
    public bool NullableRefs { get; set; }
}

/// <summary>
/// The single path from connection string to plan. `inspect` serializes this; `generate` writes
/// it. They cannot disagree, because there is only one implementation — a tool whose preview
/// contradicts its output is lying to the caller.
/// </summary>
public static class ScaffoldPipeline
{
    public static async Task<ScaffoldPlan> Build(
        ScaffoldOptions options, ProjectCapabilities capabilities, OutputLayout output,
        CancellationToken ct)
    {
        // Before anything touches the database: refusing an unimplementable option is cheaper
        // than a connection, and both `inspect` and `generate` reach it here because Build is the
        // single path. An option that cannot be honoured must never reach the point of producing
        // output that silently ignores it.
        OptionValidator.Validate(options);

        string provider = ProviderFactory.Normalize(options.Provider);

        options.ConnectionString = ResolveConnectionString(options);

        ISchemaReader reader = ProviderFactory.CreateReader(provider, options.ConnectionString);
        ITypeMapper mapper = ProviderFactory.CreateTypeMapper(provider);

        IReadOnlyList<string> available = await reader.GetNonSystemSchemas(ct);
        IReadOnlyList<string> scope = SchemaScopeResolver.Resolve(options.Schemas, available);

        DatabaseSchema schema = await reader.Read(scope, ct);

        FilterResult filtered = SchemaFilter.Apply(
            schema, scope, options.Include, options.Ignore, options.Emit);

        var plan = new ScaffoldPlan
        {
            Provider = provider,
            ServerVersion = schema.ServerVersion,
            Schemas = scope,
            Skipped = filtered.Skipped,

            // Every identifier that reaches emitted source goes through the naming layer,
            // including the ones that are not derived from a table or column: the namespace and
            // (below) the wrapper's class name and adapter property names. Escaping only the
            // names that happen to come from the schema is what left `--wrapper-class lock`
            // emitting `public partial class lock`.
            Namespace = NamingEngine.EscapeNamespace(options.Namespace)
        };

        string wrapperClassName = NamingEngine.Identifier(
            options.Wrapper.ClassName,
            "The wrapper class name (--wrapper-class / wrapper.className)",
            "Give it a name that is a legal C# type name.");

        // Resolved once and carried on the plan: the mapper, the emitted-surface computation and
        // GenerateCommand all need these, and every extra derivation is another place to drift.
        plan.NullableRefs = TriStateExtensions
            .Parse(options.Emit.NullableRefs)
            .Resolve(capabilities.NullableEnabled);

        plan.FieldKeyword = TriStateExtensions
            .Parse(options.Emit.FieldKeyword)
            .Resolve(capabilities.SupportsFieldKeyword);

        var naming = new NamingEngine(options.Naming, options.Overrides);

        // Class names are resolved for every table up front so a collision spanning any two
        // (or more) tables can be detected across the whole set, not just against the table
        // immediately before it.
        var classNames = filtered.Schema.Tables
            .Select(table => (Table: table, ClassName: naming.ClassNameFor(table, plan.Warnings)))
            .ToList();

        var built = new List<(TableInfo Table, EntityModel Entity)>();

        foreach (var (table, className) in classNames)
        {
            EntityModel entity = BuildEntity(
                table, className, naming, mapper, plan.NullableRefs, scope, options, plan);

            plan.Entities.Add(entity);
            built.Add((table, entity));

            if (!entity.IsReadOnly)
            {
                plan.Wrapper.Entries.Add(new WrapperEntry
                {
                    PropertyName = PluralPropertyName(className),
                    EntityClassName = className
                });
            }
        }

        // After the entities exist, not before: the identifiers that have to be unique are only
        // known once every column has been mapped. Nothing has been written at this point —
        // `generate` writes from plan.Emitted — so refusing here is still refusing before output.
        plan.Emitted = EmittedSurface.Of(
            built, plan.Wrapper, wrapperClassName, plan.FieldKeyword, output);

        EmittedSurface.Check(plan.Emitted, plan);

        plan.Wrapper.ClassName = wrapperClassName;
        plan.Wrapper.ConnectionName = options.Wrapper.ConnectionName;
        plan.Wrapper.Namespace = plan.Namespace;
        plan.Wrapper.PartialClasses = options.Emit.PartialClasses;

        return plan;
    }

    /// <summary>
    /// The direct <c>connectionString</c> if there is one, otherwise <c>ConnectionStrings:Zonkey</c>.
    /// </summary>
    /// <remarks>
    /// The design spec honours the named map as well as the direct value — "because .NET
    /// developers already have the muscle memory" — and it is the shape that
    /// <c>dotnet user-secrets</c> and <c>appsettings.Development.json</c> already hold, so
    /// <c>--config-file appsettings.Development.json</c> is only useful if this route works. It
    /// bound and was read by nothing, so a caller who followed the spec got "No connection
    /// string" while looking straight at their connection string.
    /// <para>
    /// The spec names exactly one key, <c>Zonkey</c>, so exactly one is honoured: a precedence
    /// list over invented names (<c>Default</c>, <c>DefaultConnection</c>, …) would be this tool
    /// guessing which of a project's databases to scaffold, which is the opposite of what it is
    /// for. The lookup is case-insensitive because the key travels through environment variables
    /// and JSON, where casing is not reliably preserved by the author's habit.
    /// </para>
    /// <para>
    /// The resolved value is written back onto <see cref="ScaffoldOptions.ConnectionString"/> by
    /// the caller rather than kept local, and that matters: <c>GenerateCommand</c> and
    /// <c>InspectCommand</c> both redact <c>--json</c> output by handing that property to
    /// <c>JsonOutput.Serialize</c> <em>after</em> this method has run. Resolving into a local
    /// would have printed a connection string that arrived through the map — password and all —
    /// verbatim into an agent transcript, defeating the spec's redaction rule through the very
    /// route being added here.
    /// </para>
    /// </remarks>
    private static string ResolveConnectionString(ScaffoldOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            return options.ConnectionString;

        foreach ((string key, string value) in options.ConnectionStrings)
            if (string.Equals(key, "Zonkey", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value))
                return value;

        throw new ScaffoldException(
            "No connection string. Set ZONKEY_SCAFFOLD_ConnectionString (preferred, keeps it " +
            "out of shell history and logs) or pass --connection. ConnectionStrings:Zonkey " +
            "(ZONKEY_SCAFFOLD_ConnectionStrings__Zonkey, or a \"connectionStrings\": " +
            "{ \"Zonkey\": ... } entry in the config file or an appsettings file layered in " +
            "with --config-file) is also honoured.");
    }

    private static EntityModel BuildEntity(
        TableInfo table, string className, NamingEngine naming, ITypeMapper mapper,
        bool nullableRefs, IReadOnlyList<string> scope, ScaffoldOptions options, ScaffoldPlan plan)
    {
        bool readOnly = table.Kind == TableKind.View || !table.HasPrimaryKey;

        if (!table.HasPrimaryKey && table.Kind == TableKind.Table)
        {
            plan.Warnings.Add(ScaffoldWarning.For(
                WarningCode.NoPrimaryKey,
                $"Table '{table.QualifiedName}' has no primary key; generated as a read-only " +
                "class. Add a key, or use overrides to map it explicitly.",
                table: table.QualifiedName));
        }

        if (table.PrimaryKey.Count > 1)
        {
            plan.Warnings.Add(ScaffoldWarning.For(
                WarningCode.CompositeKey,
                $"Table '{table.QualifiedName}' has a composite key " +
                $"({string.Join(", ", table.PrimaryKey)}); every key column is marked IsKeyField.",
                table: table.QualifiedName, level: WarningLevel.Info));
        }

        var entity = new EntityModel
        {
            ClassName = className,
            TableName = table.Name,
            // Emit SchemaName only when it carries information: a non-default schema, or a
            // multi-schema run where the bare table name would be ambiguous.
            SchemaName = scope.Count > 1 ? table.Schema : null,
            SaveToTable = LookupOverride(options, table)?.SaveToTable,
            IsReadOnly = readOnly
        };

        foreach (ColumnInfo column in table.Columns.OrderBy(c => c.Ordinal))
        {
            ColumnMapping m = mapper.Map(table, column, nullableRefs, plan.Warnings);
            string propertyName = naming.PropertyNameFor(table, column, className);

            string? forcedDbType = ForcedDbType(options, table, column);
            string dbType = forcedDbType ?? m.DbType;

            if (forcedDbType is not null)
                AnswerUnmappableWarning(plan.Warnings, table, column, m.ClrType, forcedDbType);

            string reason = forcedDbType is null
                ? m.Reason
                : $"overrides.tables.{table.Name}.columns.{column.Name}.dbType = " +
                  $"'{forcedDbType}' (mapper proposed {m.DbType}).";

            plan.Decisions.Add(new ColumnDecision
            {
                Table = table.QualifiedName,
                Column = column.Name,
                NativeType = column.NativeType,
                ProposedProperty = propertyName,
                ProposedClrType = m.ClrType,
                // The override belongs in the decision as well as in the entity: `inspect` is
                // supposed to be a preview of `generate`, so a preview that showed the mapper's
                // guess where the emitter will write the override would be lying.
                ProposedDbType = dbType,
                Reason = reason
            });

            entity.Properties.Add(new PropertyModel
            {
                Name = propertyName,
                ColumnName = column.Name,
                DbType = dbType,
                ClrType = m.ClrType,
                IsKey = table.PrimaryKey.Contains(column.Name, StringComparer.Ordinal),
                IsIdentity = column.IsIdentity,
                IsRowVersion = column.IsRowVersion,
                IsNullable = column.IsNullable,
                Length = column.MaxLength,
                DateTimeKind = m.DateTimeKind,
                SequenceName = column.SequenceName
            });
        }

        return entity;
    }

    /// <summary>
    /// Rewrites the mapper's "unrecognized type" warning for a column whose DbType an override has
    /// just supplied.
    /// </summary>
    /// <remarks>
    /// The mapper runs before the pipeline can see the override, so it warns unconditionally and
    /// tells the caller to set exactly the key they have already set. Leaving that in place is the
    /// same defect the warning's own text was just fixed for — advice for something already done —
    /// and it is the shape that makes an agent loop: act on the warning, re-run, see the identical
    /// warning.
    /// <para>
    /// Downgraded and reworded rather than deleted. The declared type really is unrecognized and
    /// the CLR property type really is still the fallback the mapper chose (the override changes
    /// the DbType only), so there is something true left to say; silence would hide it. This is a
    /// list edit rather than a redesign of how warnings flow, deliberately: threading override
    /// knowledge into <c>ITypeMapper</c> would put configuration lookup inside every future
    /// provider's mapper for one message.
    /// </para>
    /// </remarks>
    private static void AnswerUnmappableWarning(
        ICollection<ScaffoldWarning> warnings, TableInfo table, ColumnInfo column,
        string clrType, string forcedDbType)
    {
        ScaffoldWarning? answered = warnings.FirstOrDefault(w =>
            w.Code == WarningCode.UnmappableType &&
            w.Table == table.QualifiedName &&
            w.Column == column.Name);

        if (answered is null) return;

        answered.Level = WarningLevel.Info;
        answered.Message =
            $"Column '{table.Name}.{column.Name}' has unrecognized type '{column.NativeType}'. " +
            $"DbType.{forcedDbType} comes from overrides.tables.{table.Name}.columns." +
            $"{column.Name}.dbType; the CLR property type is still '{clrType}'.";
    }

    /// <summary>
    /// Applies <c>overrides.tables.&lt;t&gt;.columns.&lt;c&gt;.dbType</c>, validated against
    /// <see cref="System.Data.DbType"/>.
    /// </summary>
    /// <remarks>
    /// The setting was bound and persisted but read by nothing, which mattered more than the
    /// usual dead option because <c>SqliteTypeMapper</c> told the caller to set an override to
    /// correct an unmappable column — remediation advice that was a silent no-op. It is honoured
    /// here rather than refused because honouring it is a smaller change than removing it and
    /// leaves the mapper's advice true.
    /// <para>
    /// Validated eagerly: an unchecked value lands in the emitted source as
    /// <c>DbType.Whatever</c>, which fails to compile in the caller's project rather than in the
    /// tool, with no mention of the config key that caused it. It deliberately changes only the
    /// attribute's DbType and not the CLR property type — the two are not in general derivable
    /// from each other, and silently rewriting the property's type from a config key that says
    /// "dbType" would be its own surprise.
    /// </para>
    /// <para>
    /// <c>Enum.TryParse</c> is not that validation, which is the trap this fell into. It returns
    /// <c>true</c> for <em>any</em> numeric string — <c>"99"</c> parsed to the undefined value 99,
    /// whose <c>ToString()</c> is <c>"99"</c>, so the emitter wrote <c>DbType.99</c> at exit 0 —
    /// and it accepts a comma-separated list even on an enum carrying no <c>[Flags]</c>, ORing the
    /// members together: <c>"String,Int32"</c> is 16|11 = 27, which <em>is</em> a defined member
    /// (<c>DateTimeOffset</c>), so even an <c>Enum.IsDefined</c> guard would have let it through
    /// and silently applied a DbType nobody asked for. The only sound test is membership of
    /// <see cref="Enum.GetNames{T}"/>, matched whole, which is also what makes the returned value
    /// safe to interpolate into source: it is a name the enum actually declares.
    /// </para>
    /// </remarks>
    private static string? ForcedDbType(ScaffoldOptions options, TableInfo table, ColumnInfo column)
    {
        if (LookupOverride(options, table) is not { } to) return null;
        if (!to.Columns.TryGetValue(column.Name, out ColumnOverride? co)) return null;
        if (co.DbType is not { Length: > 0 } forced) return null;

        string? name = Enum.GetNames<System.Data.DbType>().FirstOrDefault(
            n => string.Equals(n, forced.Trim(), StringComparison.OrdinalIgnoreCase));

        if (name is null)
        {
            throw new ScaffoldException(
                $"overrides.tables.{table.Name}.columns.{column.Name}.dbType = '{forced}' is not " +
                "a member of System.Data.DbType. Valid values: " +
                string.Join(", ", Enum.GetNames<System.Data.DbType>()) + ".");
        }

        return name;
    }

    private static TableOverride? LookupOverride(ScaffoldOptions options, TableInfo table)
        => options.Overrides.Tables.TryGetValue(table.QualifiedName, out TableOverride? q) ? q
         : options.Overrides.Tables.TryGetValue(table.Name, out TableOverride? b) ? b
         : null;

    /// <summary>
    /// The wrapper's adapter property name. Escaped again after pluralizing, because the plural
    /// is a different string from the class name and can be a keyword when the class name is not:
    /// under <c>--naming-style preserve</c> a table named <c>param</c> gives the class
    /// <c>param</c> (legal) and the property <c>params</c> (not).
    /// </summary>
    private static string PluralPropertyName(string className)
        => NamingEngine.EscapeKeyword(
            new Inflector(new Dictionary<string, string>()).Pluralize(className));
}
