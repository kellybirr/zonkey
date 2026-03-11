# Code Generation Tools

The `tools/` folder contains code generators that create data class files from database schemas. These are standalone tools, not NuGet packages.

## Overview

Two generators are available:

- **Zonkey.CodeGen** -- Windows Forms GUI tool, primarily for SQL Server (via SMO)
- **NpgCodeGen** -- Console tool optimized for PostgreSQL (via Npgsql)

Both generate C# data classes with `DataItem` and `DataField` attributes, ready to use with `DataClassAdapter`.

## SQL Server Code Generator (Zonkey.CodeGen)

Located in `tools/Zonkey.CodeGen/`. A Windows Forms application that:

- Connects to SQL Server via SQL Server Management Objects (SMO)
- Browses servers, databases, tables, and views
- Generates C# (or VB.NET) data classes with full attribute decoration
- Configurable options: namespace, partial classes, virtual properties, collection types, nullable handling

Features:

- GUI for selecting tables and configuring output
- Custom property naming via delegate
- Auto-increment and primary key detection from database schema
- Row version field detection
- Schema-aware table naming
- Output to file system

## PostgreSQL Code Generator (NpgCodeGen)

Located in `tools/NpgCodeGen/`. A console application that:

- Queries PostgreSQL `information_schema` for table metadata
- Generates C# data classes
- Converts snake_case table/column names to PascalCase
- Converts plural table names to singular class names
- Detects sequences for auto-increment fields
- Handles timestamp with/without timezone (`DateTimeKind.Utc` vs `Unspecified`)
- Supports table prefix/suffix removal from property names
- Configurable table and field ignore lists

Configuration is via constants in the source code. Adjust the connection string, namespace, and naming rules before running.

## Generated Output

Both tools produce classes like:

```csharp
[DataItem("products", SchemaName = "store")]
public partial class Product : DataClass
{
    private int _id;
    private string _name = "";
    private decimal _price;
    private string? _description;

    public Product() : base(false) { }
    public Product(bool addingNew) : base(addingNew) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public virtual int Id { get => _id; set => SetFieldValue(ref _id, value); }

    [DataField("name", DbType.String, false, Length = 100)]
    public virtual string Name { get => _name; set => SetFieldValue(ref _name, value); }

    [DataField("price", DbType.Decimal, false)]
    public virtual decimal Price { get => _price; set => SetFieldValue(ref _price, value); }

    [DataField("description", DbType.String, Length = 500)]
    public virtual string? Description { get => _description; set => SetFieldValue(ref _description, value); }
}
```

Generated classes use partial classes by default, so you can add custom logic in a separate file that survives regeneration.

## When to Use Code Generation

- Initial setup of a new project with an existing database
- Adding new tables to an existing project
- Regenerating after schema changes

After generation, you can customize the classes (add OnBeforeSave hooks, computed properties, etc.) in a separate partial class file.

## Alternative: Manual Class Creation

For small projects or when you want full control, write data classes by hand. The attribute syntax is straightforward and well-supported by IDE tooling.

---

[Back to documentation index](README.md) | [Data Classes & Attributes](data-classes.md) | [Getting Started](getting-started.md)
