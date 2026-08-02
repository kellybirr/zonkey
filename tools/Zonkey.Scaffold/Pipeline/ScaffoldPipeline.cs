using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Naming;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Providers;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Pipeline;

public sealed class ScaffoldPlan
{
    public string Provider { get; set; } = "";
    public string ServerVersion { get; set; } = "";
    public string? Namespace { get; set; }
    public List<EntityModel> Entities { get; set; } = new();
    public WrapperModel Wrapper { get; set; } = new();

    /// <summary>Things the caller may want to look at. Plain strings — the output is a draft.</summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>Connection string in, plan out.</summary>
public static class ScaffoldPipeline
{
    public static async Task<ScaffoldPlan> Build(ScaffoldOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ScaffoldException("No connection string. Pass --connection or set ConnectionString.");

        string provider = ProviderFactory.Normalize(options.Provider);
        ISchemaReader reader = ProviderFactory.CreateReader(provider, options.ConnectionString);
        ITypeMapper mapper = ProviderFactory.CreateTypeMapper(provider);
        var naming = new NamingEngine(options.Naming);

        IReadOnlyList<string> scope = options.Schemas.Count > 0
            ? options.Schemas
            : await reader.GetNonSystemSchemas(ct);

        DatabaseSchema schema = await reader.Read(scope, ct);

        var plan = new ScaffoldPlan
        {
            Provider = provider,
            ServerVersion = schema.ServerVersion,
            Namespace = NamingEngine.EscapeNamespace(options.Namespace),
        };

        bool multiSchema = scope.Count > 1;

        foreach (TableInfo table in schema.Tables)
        {
            if (table.Kind == TableKind.View && !options.Views) continue;
            if (IsIgnored(table.Name, options.IgnoreTables)) continue;

            string className = naming.ClassNameFor(table);

            var entity = new EntityModel
            {
                ClassName = className,
                TableName = table.Name,
                SchemaName = multiSchema ? table.Schema : null,
                IsReadOnly = table.Kind == TableKind.View || !table.HasPrimaryKey,
                Namespace = plan.Namespace,
            };

            foreach (ColumnInfo column in table.Columns)
            {
                ColumnMapping m = mapper.Map(table, column, options.Emit.NullableRefs, plan.Warnings);

                entity.Properties.Add(new PropertyModel
                {
                    Name = naming.PropertyNameFor(table, column),
                    ColumnName = column.Name,
                    DbType = m.DbType,
                    ClrType = m.ClrType,
                    DateTimeKind = m.DateTimeKind,
                    IsKey = table.PrimaryKey.Contains(column.Name, StringComparer.Ordinal),
                    IsIdentity = column.IsIdentity,
                    IsRowVersion = column.IsRowVersion,
                    IsNullable = column.IsNullable,
                    Length = column.MaxLength,
                    SequenceName = column.SequenceName,
                });
            }

            plan.Entities.Add(entity);
        }

        var inflector = new Inflector(options.Naming.Irregulars);

        plan.Wrapper = new WrapperModel
        {
            ClassName = NamingEngine.Identifier(
                options.Wrapper.ClassName, "The wrapper class name", "Give it a legal C# type name."),
            ConnectionName = options.Wrapper.ConnectionName,
            Namespace = plan.Namespace,
            PartialClasses = options.Emit.PartialClasses,
            Entries = plan.Entities.Select(e => new WrapperEntry
            {
                PropertyName = NamingEngine.EscapeKeyword(inflector.Pluralize(e.ClassName)),
                EntityClassName = e.ClassName,
            }).ToList(),
        };

        return plan;
    }

    /// <summary>Exact match, or a trailing <c>*</c> prefix match. Case-insensitive.</summary>
    private static bool IsIgnored(string tableName, List<string> patterns)
        => patterns.Any(p => p.EndsWith('*')
            ? tableName.StartsWith(p[..^1], StringComparison.OrdinalIgnoreCase)
            : tableName.Equals(p, StringComparison.OrdinalIgnoreCase));
}
