# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Zonkey is a .NET ORM library that maps database tables to C# classes. It supports multiple database dialects and targets netstandard2.0, netstandard2.1, net6.0, net8.0, and net48.

NuGet packages: zonkey.data, zonkey.text, zonkey.droid, zonkey.mocks

## Build & Test Commands

```bash
# Build entire solution
dotnet build "Zonkey 5.0.sln"

# Build a specific project
dotnet build src/Zonkey.Data/Zonkey.Data.csproj

# Run all tests
dotnet test test/UnitTests.Core/UnitTests.csproj

# Run a single test
dotnet test test/UnitTests.Core/UnitTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run tests for a specific framework
dotnet test test/UnitTests.Core/UnitTests.csproj -f net8.0
```

Tests use MSTest and reference AdventureWorks sample database objects.

## Solution Structure

- **src/Zonkey.Data** — Core ORM library (main package)
- **src/Zonkey.Data.MsSql** — SQL Server-specific extensions (depends on Microsoft.Data.SqlClient)
- **src/Zonkey.Text** — CSV/text file reader/writer
- **src/Zonkey.Mocks** — Mock ADO.NET objects for unit testing
- **test/UnitTests.Core** — MSTest test suite

## Architecture

### Data Class Model
- `DataClass` — Base class for mapped entities. Tracks changes via `DataRowState` and uses `SetFieldValue<T>()` with `[CallerMemberName]` for property change tracking.
- `DataItemAttribute` — Marks a class as a data entity, specifies table name, schema, and access type.
- `DataFieldAttribute` — Maps a property to a database column with options: `IsKeyField`, `IsAutoIncrement`, `IsNullable`, `IsRowVersion`, `IsPartitionKey`, `FieldName`, `DataType`, etc.

### DataMap (ObjectModel/DataMap.cs)
Static reflection cache (`_mapCache`) that analyzes attributed properties to build field-to-column mappings. Categorizes fields into readable, writable, key, and partition key sets.

### DataClassAdapter<T> (DataClassAdapter/)
Generic adapter handling CRUD operations. Split across multiple files by operation type: Fill.cs, Save.cs, Delete.cs, BulkInsert.cs, BulkUpdate.cs, GetSingleItem.cs, Populate.cs, etc.

### DatabaseWrapper (ObjectModel/DatabaseWrapper.cs)
Abstract base class for application-specific database access. Caches DataClassAdapter instances per type and provides transaction support. This is the preferred entry point for consuming code.

### DataManager (DataManager.cs)
Ad-hoc query execution. Manages connections, dialect selection, and parameterized queries.

### SqlFilter (SqlFilter.cs)
Fluent API for building parameterized WHERE clauses: `EQ()`, `NEQ()`, `GT()`, `LT()`, `LIKE()`, etc.

### Database Dialects (Dialects/)
Pluggable SQL generation per database: SqlServer, Sqlite, MySql, PostgreSql, Oracle, Db2, Access, and a generic/ANSI fallback. Each handles identifier quoting, paging, parameter naming, etc.

### DataClassCommandBuilder (ObjectModel/DataClassCommandBuilder/)
Generates SELECT, INSERT, UPDATE, DELETE SQL commands. Split by command type in separate files.

### WhereExpressionParser
Converts LINQ expressions into SQL WHERE clauses for type-safe filtering.

## Key Patterns

- **Multi-targeting**: All projects target multiple frameworks with conditional compilation. Watch for `#if` directives.
- **Reflection caching**: DataMap caches are static and built once per type.
- **Dynamic IL**: ClassFactory uses `DynamicMethod`/IL emit for fast object instantiation.
- **Async throughout**: Async methods use the `TaskAsync` suffix convention (e.g., `FillTaskAsync`).
- **Strong naming**: Projects use strong-name signing.
