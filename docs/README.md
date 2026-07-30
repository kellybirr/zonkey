# Zonkey Documentation

Zonkey is a deterministic, explicit, async-first ORM for .NET. It maps C# objects to database tables using attributes, giving you full control over every database operation with no hidden behavior.

For a high-level introduction, see the [Overview & Philosophy](overview.md).

## Table of Contents

- [Overview & Philosophy](overview.md) -- Core values, architecture, and design principles
- [Architecture](architecture.md) -- The life of a query and a save: how the layers connect and why
- [Getting Started](getting-started.md) -- Installation, setup, and first query
- [Data Classes & Attributes](data-classes.md) -- Object-to-table mapping with `DataItem` and `DataField`
- [DataClassAdapter (CRUD Operations)](data-class-adapter.md) -- Reading, saving, and deleting mapped objects
- [Querying (Filters, LINQ, Pagination)](querying.md) -- Lambda expressions, `SqlFilter`, and filtering patterns
- [Modeling Relationships](modeling-relationships.md) -- Related data without navigation properties: explicit loading, stitching, and graph saves
- [DatabaseWrapper (Connection & Lifecycle)](database-wrapper.md) -- Connection management and adapter caching
- [DataManager (Raw SQL)](data-manager.md) -- Ad-hoc queries, stored procedures, and scalar operations
- [Async Patterns](async-patterns.md) -- Async-first design and task-based operations
- [Transactions](transactions.md) -- Simple and distributed transaction support
- [DataTableAdapter (DataTable & DataSet)](data-table-adapter.md) -- Working with `DataTable` and `DataSet`
- [Recordset (Classic ADO Style)](recordset.md) -- Cursor-style API for code migrated from classic ADO
- [Database Providers & Dialects](database-providers.md) -- SQL Server, PostgreSQL, MySQL, Oracle, SQLite, and more
- [PostgreSQL Guide](postgresql.md) -- timestamp/timestamptz mapping, case folding, NativeType and provider-specific types
- [Testing with Zonkey.Mocks](testing.md) -- Mocking adapters and connections for unit tests
- [Text File Mapping (Zonkey.Text)](text-files.md) -- CSV and fixed-width text file mapping
- [Code Generation Tools](code-generation.md) -- Generating data classes from database schemas
- [Migrating from Entity Framework](migrating-from-ef.md) -- Concept mapping and migration guide

## NuGet Packages

| Package | Description |
|---|---|
| **Zonkey.Data** | Core library. Contains `DataClass`, `DataClassAdapter`, `DatabaseWrapper`, `DataManager`, `SqlFilter`, dialect system, and all mapping attributes. This is the only package required for most projects. |
| **Zonkey.Data.MsSql** | SQL Server-specific extensions, including `SqlXmlAdapter` for XML column support and SQL Server-optimized operations. |
| **Zonkey.Text** | Text file mapping. Provides `TextClassReader`, `TextClassWriter`, and `CsvReader` for reading and writing CSV and fixed-width text files using the same attribute-based mapping approach as the database layer. |
| **Zonkey.Mocks** | Testing support. Provides mock implementations of adapters and connections for unit testing without a database. |

## Project README

See the [project README](../README.md) for installation instructions and a quick overview.
