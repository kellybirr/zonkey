namespace Zonkey.Scaffold.Selection;

public sealed class SkipRecord
{
    public string Table { get; set; } = "";
    public string? Column { get; set; }
    public string Reason { get; set; } = "";
    public string Pattern { get; set; } = "";
}

public sealed class FilterResult
{
    public required Zonkey.Scaffold.Schema.DatabaseSchema Schema { get; init; }
    public List<SkipRecord> Skipped { get; init; } = new();
}
