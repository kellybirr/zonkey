namespace Zonkey.Scaffold.Emit;

/// <summary>
/// Translates the type mapper's C# type names into VB.
/// </summary>
/// <remarks>
/// The type mappers speak C# because that is what they were written for, and one shared mapping
/// table here is cheaper than a second mapper per provider. VB has no nullable reference types, so
/// a trailing <c>?</c> is kept only on value types — <c>String?</c> is not VB.
/// </remarks>
public static class VbTypeNames
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["bool"] = "Boolean",
        ["byte"] = "Byte",
        ["sbyte"] = "SByte",
        ["short"] = "Short",
        ["ushort"] = "UShort",
        ["int"] = "Integer",
        ["uint"] = "UInteger",
        ["long"] = "Long",
        ["ulong"] = "ULong",
        ["float"] = "Single",
        ["double"] = "Double",
        ["decimal"] = "Decimal",
        ["char"] = "Char",
        ["string"] = "String",
        ["object"] = "Object",
        ["byte[]"] = "Byte()",
        ["DateTime"] = "Date",
        ["DateTimeOffset"] = "DateTimeOffset",
        ["TimeSpan"] = "TimeSpan",
        ["Guid"] = "Guid",
        ["DateOnly"] = "DateOnly",
        ["TimeOnly"] = "TimeOnly",
    };

    /// <summary>The set of C# names that name a value type, so <c>?</c> survives translation.</summary>
    private static readonly HashSet<string> ValueTypes = new(StringComparer.Ordinal)
    {
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "decimal", "char", "DateTime", "DateTimeOffset", "TimeSpan",
        "Guid", "DateOnly", "TimeOnly",
    };

    public static string Translate(string csharpType)
    {
        bool nullable = csharpType.EndsWith('?');
        string bare = nullable ? csharpType[..^1] : csharpType;

        string vb = Map.TryGetValue(bare, out string? mapped) ? mapped : bare;

        return nullable && ValueTypes.Contains(bare) ? vb + "?" : vb;
    }

    /// <summary>True when the property must be declared without a nullable marker in VB.</summary>
    public static bool IsReferenceType(string csharpType)
        => !ValueTypes.Contains(csharpType.TrimEnd('?'));
}
