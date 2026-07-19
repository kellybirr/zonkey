# Code Generation Tools

The `tools/` folder contains code generators that create data class files from database schemas. These are standalone tools, not NuGet packages. They are legacy .NET Framework projects that are not part of `Zonkey.sln` and are not built by `dotnet build`.

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
- Marks auto-increment fields from the schema's `IsAutoIncrement` metadata (explicit sequence-name wiring exists in the source but is commented out)
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

## Runtime Code Generation (ClassFactory)

Separate from the schema-scaffolding tools above, Zonkey also generates a small amount of code at runtime. `Zonkey.ObjectModel.ClassFactory` emits a `DynamicMethod` (just `newobj`/`ret` IL) for each mapped type and caches the resulting factory delegate per type. Fill operations and `DataClassReader` construct one object per row, so a compiled factory keeps `Activator.CreateInstance`-style reflection cost from dominating materialization.

Callers can override how objects are created:

- `ClassFactory.RegisterType(...)` -- supply a custom factory delegate for a type
- `ClassFactory.RegisterInterface<TInterface, TConcrete>(...)` -- map an interface to a concrete class so it can be materialized

The emitted factory requires a public parameterless constructor. Types without one must have a factory registered with `ClassFactory`, or set `ObjectFactory` on the `DataClassAdapter<T>` instance.

`DataClassReader<T>` goes a step further with its **fast builder** (on by default): a `DynamicMethod` emitted once per (type, result-set shape) that populates an entire row with straight-line IL -- null-check, convert, and set per mapped column, with all conversion decisions resolved at emit time from the reader's known column types. Conversion failures throw `PropertyReadException` identifying the property. See [Architecture](architecture.md#6-rows-become-objects) for the full walkthrough; `UseFastBuilder = false` selects the per-field reflection path.

---

[Back to documentation index](README.md) | [Data Classes & Attributes](data-classes.md) | [Getting Started](getting-started.md)
