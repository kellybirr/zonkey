namespace Zonkey.Scaffold.Options;

public sealed class ScaffoldOptions
{
    public string? Provider { get; set; }
    public string Language { get; set; } = "csharp";
    public string? ConnectionString { get; set; }
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
    public string? Namespace { get; set; }
    public List<string> Schemas { get; set; } = new();
    public string SchemaDisambiguation { get; set; } = "none";

    public OutputOptions Output { get; set; } = new();
    public WrapperOptions Wrapper { get; set; } = new();
    public NamingOptions Naming { get; set; } = new();
    public EmitOptions Emit { get; set; } = new();
    public SelectionOptions Include { get; set; } = new();
    public IgnoreOptions Ignore { get; set; } = new();
    public OverrideOptions Overrides { get; set; } = new();
}

public sealed class OutputOptions
{
    public string Entities { get; set; } = ".";
    public string? Wrapper { get; set; }
    public bool GeneratedSuffix { get; set; } = true;
}

public sealed class WrapperOptions
{
    public string ClassName { get; set; } = "AppDatabase";
    public string ConnectionName { get; set; } = "Default";
}

public sealed class NamingOptions
{
    public string Style { get; set; } = "pascal";           // pascal | preserve
    public bool Singularize { get; set; } = true;
    public Dictionary<string, string> Irregulars { get; set; } = new();
    public string ClassPrefix { get; set; } = "";
    public string ClassSuffix { get; set; } = "";
    public bool StripClassName { get; set; } = false;
}

public sealed class EmitOptions
{
    public bool PartialClasses { get; set; } = true;
    public string FieldKeyword { get; set; } = "auto";      // TriState text
    public string NullableRefs { get; set; } = "auto";      // TriState text
    public bool PrivateFieldsAtTop { get; set; } = false;
    public bool VirtualProperties { get; set; } = false;
    public string Collections { get; set; } = "none";       // none|generic|dataclass|bindable
    public bool TypedAdapters { get; set; } = false;
    public bool Relations { get; set; } = false;
    public bool Views { get; set; } = false;
    public bool SystemTables { get; set; } = false;
}

public sealed class SelectionOptions
{
    public List<string> Tables { get; set; } = new();
}

public sealed class IgnoreOptions
{
    public List<string> Tables { get; set; } = new();
    public List<string> Columns { get; set; } = new();      // "table.column" glob patterns
}

public sealed class OverrideOptions
{
    public Dictionary<string, TableOverride> Tables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TableOverride
{
    public string? ClassName { get; set; }
    public string? SaveToTable { get; set; }
    public Dictionary<string, ColumnOverride> Columns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ColumnOverride
{
    public string? Property { get; set; }
    public string? DbType { get; set; }
}
