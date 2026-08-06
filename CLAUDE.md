# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Zonkey is a .NET ORM library that maps database tables to C# classes. It supports multiple database dialects and targets net8.0, net10.0, and net48 (Zonkey.Text targets netstandard2.0 and net48).

NuGet packages: zonkey.data, zonkey.data.mssql, zonkey.text, zonkey.mocks, plus the zonkey.scaffold dotnet tool

## Build & Test Commands

```bash
# Build entire solution
dotnet build Zonkey.sln

# Build a specific project
dotnet build src/Zonkey.Data/Zonkey.Data.csproj

# Run all tests (unit + SQLite integration; MSSQL/PostgreSQL tests skip if containers are down)
dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj

# Run a single test
dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run tests for a specific framework
dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj -f net10.0

# Start MSSQL (host port 1434), PostgreSQL (host port 5433), and MySQL (host port 3308) for integration tests
docker compose up -d --wait
```

Tests use xUnit v3 with a zoo-themed schema (Animal, Species, Exhibit, Zookeeper, FeedingSchedule). SQLite integration tests always run (temp-file DB); MSSQL, PostgreSQL, and MySQL integration tests run against the docker-compose containers and skip gracefully when unavailable. Connection strings can be overridden with the `ZONKEY_TEST_MSSQL`, `ZONKEY_TEST_PGSQL`, and `ZONKEY_TEST_MYSQL` environment variables. On net48 only unit tests run.

## Solution Structure

- **src/Zonkey.Data** — Core ORM library (main package)
- **src/Zonkey.Data.MsSql** — SQL Server-specific extensions (depends on Microsoft.Data.SqlClient)
- **src/Zonkey.Text** — CSV/text file reader/writer
- **src/Zonkey.Mocks** — Mock ADO.NET objects for unit testing
- **tools/Zonkey.Scaffold** — `zonkey-scaffold`, the CLI that generates data classes from a live database
- **test/Zonkey.Tests** — xUnit v3 test suite (unit + SQLite/MSSQL/PostgreSQL/MySQL integration)
- **test/Zonkey.Scaffold.Tests** — scaffold tests (live-database smoke tests across four providers)

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
- **Dynamic IL**: ClassFactory uses `DynamicMethod`/IL emit for fast object instantiation, and DataClassReader's fast builder (default on) emits a per-result-set-shape populator; conversion failures surface as `PropertyReadException`.
- **Async throughout**: All data-access methods are async and deliberately suffix-less (`Fill`, `Save`, `GetOne` — not `FillAsync`). There are no sync variants to disambiguate from; do not rename or add `-Async` suffixes.
- **Strong naming**: Removed in v7.0. Tests access internals via `InternalsVisibleTo("Zonkey.Tests")`.
