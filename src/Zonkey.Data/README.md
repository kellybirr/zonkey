# Zonkey.Data

The core Zonkey ORM library. Provides object-relational mapping, querying, persistence, and multi-database support for .NET applications.

## Installation

```shell
dotnet add package Zonkey.Data
```

## What's Inside

- **DataClassAdapter&lt;T&gt;** -- typed CRUD operations for mapped classes
- **DataClass** -- optional base class with per-object change tracking
- **DataItemAttribute / DataFieldAttribute** -- attribute-based table and column mapping
- **DatabaseWrapper** -- abstract base class for connection lifecycle management
- **DataManager** -- raw SQL execution (scalar, non-query, reader, DataSet)
- **SqlFilter** -- fluent, parameterized WHERE clause building
- **DataTableAdapter** -- CRUD operations for DataTable/DataSet
- **Dialect system** -- automatic SQL generation for SQL Server, PostgreSQL, MySQL, SQLite, Oracle, DB2, and more

## Target Frameworks

- .NET Standard 2.0
- .NET Standard 2.1
- .NET 6
- .NET 8
- .NET Framework 4.8

## Key Concepts

Every database operation is explicit. You control when queries execute, what gets saved, and how connections are managed. There is no ambient context, no lazy loading, and no implicit persistence.

Change tracking is per-object through `DataRowState` (Added, Modified, Unchanged, Detached) and is only active when your classes inherit from `DataClass`.

## Quick Example

```csharp
var adapter = new DataClassAdapter<Product>(connection);

// Query
var product = await adapter.GetOne(p => p.Id == 42);

// Modify
product.Price = 19.99m;

// Save -- only the price column is updated
await adapter.Save(product);
```

## Documentation

See the [full documentation](../../docs/) for detailed guides on all features.
