namespace Zonkey.Scaffold.Schema;

public enum TableKind { Table, View }

public sealed class DatabaseSchema
{
    public string Provider { get; set; } = "";
    public string ServerVersion { get; set; } = "";
    public List<TableInfo> Tables { get; set; } = new();
}

public sealed class TableInfo
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public TableKind Kind { get; set; } = TableKind.Table;
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<string> PrimaryKey { get; set; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    public List<UniqueConstraintInfo> UniqueConstraints { get; set; } = new();

    public string QualifiedName =>
        string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";

    public bool HasPrimaryKey => PrimaryKey.Count > 0;
}

public sealed class ColumnInfo
{
    public string Name { get; set; } = "";
    public string NativeType { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsRowVersion { get; set; }

    // MySQL-specific: true for e.g. "int unsigned". NativeType stays the plain data_type spelling
    // ("int"); sign is a separate axis a type mapper needs independently, since it changes which
    // CLR integer type is safe to use without narrowing the column's actual range. Always false
    // for providers that have no unsigned integer types (everything except MySQL/MariaDB).
    public bool IsUnsigned { get; set; }

    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public string? SequenceName { get; set; }
    public int Ordinal { get; set; }
}

public sealed class ForeignKeyInfo
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public string ReferencedSchema { get; set; } = "";
    public string ReferencedTable { get; set; } = "";
    public List<string> ReferencedColumns { get; set; } = new();
}

public sealed class UniqueConstraintInfo
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = new();
}
