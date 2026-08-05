namespace Zonkey.Scaffold.Options;

/// <summary>
/// Everything the tool can be told, bound straight from IConfiguration (JSON file, then
/// ZONKEY_SCAFFOLD_* environment variables, then the command line).
/// </summary>
public sealed class ScaffoldOptions
{
    public string? Provider { get; set; }
    public string? ConnectionString { get; set; }
    public string? Namespace { get; set; }

    /// <summary><c>CSharp</c> or <c>VB</c>.</summary>
    public string Language { get; set; } = "CSharp";

    /// <summary>Schemas to read. Empty means every non-system schema.</summary>
    public List<string> Schemas { get; set; } = new();

    /// <summary>Table names to skip. Supports a trailing <c>*</c>.</summary>
    public List<string> IgnoreTables { get; set; } = new();

    public bool Views { get; set; }
    public bool DryRun { get; set; }

    public OutputOptions Output { get; set; } = new();
    public WrapperOptions Wrapper { get; set; } = new();
    public NamingOptions Naming { get; set; } = new();
    public EmitOptions Emit { get; set; } = new();
}

public sealed class OutputOptions
{
    public string Entities { get; set; } = ".";
    public string? Wrapper { get; set; }

    /// <summary>Write <c>.g.cs</c> rather than <c>.cs</c>.</summary>
    public bool GeneratedSuffix { get; set; } = true;
}

public sealed class WrapperOptions
{
    public string ClassName { get; set; } = "AppDatabase";
    public string ConnectionName { get; set; } = "Default";
}

public sealed class NamingOptions
{
    /// <summary><c>pascal</c> or <c>preserve</c>.</summary>
    public string Style { get; set; } = "pascal";

    public bool Singularize { get; set; } = true;

    /// <summary>Plural-to-singular pairs the inflector gets wrong, e.g. <c>"data": "datum"</c>.</summary>
    public Dictionary<string, string> Irregulars { get; set; } = new();
}

public sealed class EmitOptions
{
    public bool PartialClasses { get; set; } = true;
    public bool VirtualProperties { get; set; }

    /// <summary>Use the C# <c>field</c> keyword instead of declaring backing fields.</summary>
    public bool FieldKeyword { get; set; } = true;

    /// <summary>Only meaningful when <see cref="FieldKeyword"/> is off.</summary>
    public bool PrivateFieldsAtTop { get; set; }

    /// <summary>Ignored for VB, which has no nullable reference types.</summary>
    public bool NullableRefs { get; set; } = true;

    /// <summary>
    /// Emit in-memory graph members for foreign keys. Opt-in, and they load nothing by themselves —
    /// see docs/modeling-relationships.md.
    /// </summary>
    public bool Relations { get; set; }
}
