namespace Zonkey.Scaffold.Schema;

public interface ISchemaReader
{
    /// <summary>Provider key as it appears in config, e.g. "sqlite".</summary>
    string Provider { get; }

    /// <summary>Schemas a user could reasonably generate from, excluding catalog schemas.</summary>
    Task<IReadOnlyList<string>> GetNonSystemSchemas(CancellationToken ct);

    /// <summary>Reads the full schema for the given scope. Output ordering must be deterministic.</summary>
    Task<DatabaseSchema> Read(IReadOnlyList<string> schemas, CancellationToken ct);
}
