namespace Zonkey.Scaffold.Diagnostics;

public static class WarningLevel
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}

public static class WarningCode
{
    public const string NoPrimaryKey          = "NO_PRIMARY_KEY";
    public const string CompositeKey          = "COMPOSITE_KEY";
    public const string UnmappableType        = "UNMAPPABLE_TYPE";
    public const string ReservedWord          = "RESERVED_WORD";
    public const string MixedCaseIdentifier   = "MIXED_CASE_IDENTIFIER";
    public const string InflectionUncertain   = "INFLECTION_UNCERTAIN";
    public const string ClassNameCollision    = "CLASS_NAME_COLLISION";
    public const string HidesBaseMember       = "HIDES_BASE_MEMBER";

    // Two codes rather than one with two messages, because the consequences differ in kind and a
    // consumer may reasonably act on them differently: SHADOWS_BASE_TYPE is a build failure the
    // caller cannot miss, SHADOWS_REFERENCED_TYPE is code that builds and means something other
    // than it says. An agent triaging --json output should be able to tell those apart without
    // parsing prose.
    public const string ShadowsBaseType       = "SHADOWS_BASE_TYPE";
    public const string ShadowsReferencedType = "SHADOWS_REFERENCED_TYPE";
}

public sealed class ScaffoldWarning
{
    public string Code { get; set; } = "";
    public string Level { get; set; } = WarningLevel.Warning;
    public string? Table { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = "";

    public static ScaffoldWarning For(string code, string message,
        string? table = null, string? column = null, string level = WarningLevel.Warning)
        => new() { Code = code, Message = message, Table = table, Column = column, Level = level };
}
