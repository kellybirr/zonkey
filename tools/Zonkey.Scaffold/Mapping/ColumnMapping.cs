namespace Zonkey.Scaffold.Mapping;

/// <summary>
/// A mapping decision plus the sentence explaining it. The reason is what makes `inspect`
/// a scaffolding oracle rather than a schema dumper — it is surfaced verbatim in JSON.
/// </summary>
public sealed class ColumnMapping
{
    public required string DbType { get; init; }
    public required string ClrType { get; init; }
    public required string Reason { get; init; }
    public string? DateTimeKind { get; init; }
}
