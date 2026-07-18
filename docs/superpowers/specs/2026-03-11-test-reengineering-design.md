# Zonkey Test Reengineering Design Spec

**Date:** 2026-03-11
**Status:** Draft
**Scope:** Complete rewrite of the Zonkey test suite

## Problem Statement

The existing test suite depends on a local SQL Server installation with the AdventureWorks 2014 database. Connection strings are hardcoded to specific developer machines. Most integration tests are `[Ignore]`d because the required database is rarely available. CI only runs tests during release builds. The result is that the test suite provides little confidence and is effectively unmaintained.

## Goals

1. Any developer can run meaningful tests locally with zero setup beyond `dotnet test`
2. Integration tests cover all three supported database providers (SQLite, MSSQL, PostgreSQL)
3. Tests run automatically on every PR and push to master
4. Clear separation between tests that need a database and tests that don't
5. Test data is deterministic, fun (zoo-themed), and small enough to reason about

## Non-Goals

- Testing stored procedure support (not portable across providers)
- Testing SQL Server change tracking context (provider-specific)
- Testing OleDb/Odbc connection factories (legacy, net48-only)
- Testing SqlXmlAdapter (niche feature)
- Testing InnerJoin (can be added later)
- Testing `IsPartitionKey` (not exercised in zoo model; can add a table later if needed)
- Migrating existing `Zonkey.Text` CSV tests or `Zonkey.Mocks` mock tests (these are niche; the rewrite focuses on core ORM coverage. If desired, they can be ported to xUnit in a follow-up)

## Architecture

### Single Test Project

One xUnit test project (`Zonkey.Tests`) targeting `net10.0` and `net48`. This replaces the existing `UnitTests.Core` MSTest project.

The project contains two categories of tests:

- **Unit tests** — pure logic tests with no database dependency. These run on all platforms and all target frameworks.
- **Integration tests** — database-backed tests using the generic base class pattern. Written once, executed per database provider via thin concrete subclasses.

### Generic Base Class Pattern

Integration test logic is written in abstract generic base classes parameterized by a database fixture type:

```csharp
public abstract class CrudTests<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IDatabaseFixture
{
    protected readonly TFixture Db;
    public CrudTests(TFixture db) => Db = db;

    [Fact]
    public async Task CanInsertAndRetrieveAnimal()
    {
        if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
        // test logic using Db.GetConnection() and Db.Dialect
    }
}
```

Thin concrete classes instantiate the base for each provider:

```csharp
public class SqliteCrudTests : CrudTests<SqliteFixture>
{
    public SqliteCrudTests(SqliteFixture db) : base(db) { }
}

public class MssqlCrudTests : CrudTests<MssqlFixture>
{
    public MssqlCrudTests(MssqlFixture db) : base(db) { }
}

public class PgsqlCrudTests : CrudTests<PgsqlFixture>
{
    public PgsqlCrudTests(PgsqlFixture db) : base(db) { }
}
```

### Database Fixture Infrastructure

```
IDatabaseFixture : IAsyncDisposable
    ├── IsAvailable          → bool, false if database unreachable
    ├── SkipReason           → string message for skipped tests
    ├── Dialect              → SqlDialect for this provider (manually instantiated)
    ├── SupportsRowVersion   → bool, whether concurrency tests should run
    ├── GetConnection()      → returns open DbConnection
    ├── InitializeAsync()    → create temp DB, run seed SQL
    └── DisposeAsync()       → drop temp DB, close connections
```

The `Dialect` property is **manually instantiated** by each fixture (e.g., `new SqliteDialect()`, `new SqlServerDialect()`, `new PostgreSqlDialect()`) rather than relying on `SqlDialect.Create()` auto-detection. This avoids issues with unregistered connection types (see Required Source Changes below).

**SqliteFixture:** Creates a temp `.db` file via `Path.GetTempPath()`. Runs `sqlite-seed.sql` to create schema and insert data. Deletes file on dispose. Always available on all platforms. `SupportsRowVersion = false`.

**MssqlFixture:** Connects using default `localhost,1433` or the `ZONKEY_TEST_MSSQL` environment variable. Creates a `zonkey_test_{guid}` database, runs `mssql-seed.sql`, drops database on dispose. Sets `IsAvailable = false` if connection fails. `SupportsRowVersion = true`.

**PgsqlFixture:** Connects using default `localhost:5432` or the `ZONKEY_TEST_PGSQL` environment variable. Creates a `zonkey_test_{guid}` database, runs `pgsql-seed.sql`, drops database on dispose. Sets `IsAvailable = false` if connection fails. `SupportsRowVersion = false` (PostgreSQL `xmin` support is a future enhancement).

Each fixture creates a fresh database per test class (xUnit `IClassFixture` lifecycle). Schema + seed for 5 small tables is fast.

### Connection Configuration

Default connection strings match the docker-compose setup. No configuration file needed for the common case.

| Provider | Default Connection | Env Var Override |
|----------|-------------------|-----------------|
| SQLite | `Data Source={tempfile}` | n/a |
| MSSQL | `Server=localhost,1433;User=sa;Password=Zonkey#Test123;TrustServerCertificate=true` | `ZONKEY_TEST_MSSQL` |
| PostgreSQL | `Host=localhost;Port=5432;Username=zonkey;Password=zonkey;Database=zonkey_test` | `ZONKEY_TEST_PGSQL` |

## Data Model

Zoo-themed dataset with 5 tables designed to exercise all key ORM features.

### Species

| Column | Type | ORM Feature |
|--------|------|-------------|
| SpeciesId | int, PK, auto-increment | `IsKeyField`, `IsAutoIncrement` |
| Name | varchar(100), not null | basic string |
| Classification | varchar(50), nullable | `IsNullable` |
| IsEndangered | bit/bool, not null | boolean handling |

### Exhibit

| Column | Type | ORM Feature |
|--------|------|-------------|
| ExhibitId | int, PK, auto-increment | `IsKeyField`, `IsAutoIncrement` |
| Name | varchar(100), not null | basic string |
| Location | varchar(200), nullable | nullable string |
| Capacity | int, not null | integer |
| IsOpen | bit/bool, not null, default true | boolean with default |
| RowVersion | rowversion (MSSQL only, omitted on SQLite/PostgreSQL) | `IsRowVersion` for optimistic concurrency |

### Zookeeper

| Column | Type | ORM Feature |
|--------|------|-------------|
| ZookeeperId | uniqueidentifier/uuid, PK | GUID key (not auto-increment) |
| FirstName | varchar(50), not null | string |
| LastName | varchar(50), not null | string |
| Email | varchar(200), nullable | nullable |
| HireDate | date, not null | date handling |
| Specialty | varchar(100), nullable | nullable string |

### Animal

| Column | Type | ORM Feature |
|--------|------|-------------|
| AnimalId | int, PK, auto-increment | `IsKeyField`, `IsAutoIncrement` |
| Name | varchar(100), not null | string |
| SpeciesId | int, FK, not null | foreign key |
| ExhibitId | int, FK, nullable | nullable FK |
| ZookeeperId | uniqueidentifier/uuid, FK, not null | FK to GUID key |
| DateOfBirth | datetime, nullable | nullable datetime |
| Weight | decimal(8,2), nullable | nullable decimal |
| Notes | text, nullable | long text, `IsComparable = false` |

### FeedingSchedule (composite key)

| Column | Type | ORM Feature |
|--------|------|-------------|
| AnimalId | int, PK, FK | composite key part 1 |
| DayOfWeek | int, PK | composite key part 2 |
| TimeSlot | varchar(10), PK | composite key part 3 |
| FoodType | varchar(100), not null | string |
| Quantity | decimal(6,2), not null | decimal |
| AssignedKeeperId | uniqueidentifier/uuid, FK, nullable | nullable GUID FK |

### ORM Feature Coverage Matrix

| Feature | Table |
|---------|-------|
| Auto-increment int PK | Species, Exhibit, Animal |
| GUID PK (non-auto) | Zookeeper |
| Composite PK (3 fields) | FeedingSchedule |
| RowVersion / optimistic concurrency | Exhibit (MSSQL only) |
| Nullable fields | all tables |
| IsComparable = false | Animal.Notes |
| Boolean fields | Species.IsEndangered, Exhibit.IsOpen |
| Date / DateTime | Zookeeper.HireDate, Animal.DateOfBirth |
| Decimal | Animal.Weight, FeedingSchedule.Quantity |
| Foreign keys | Animal, FeedingSchedule |

### Seed Data

Deterministic, small, sufficient for all query patterns:

- **3 species:** Red Panda, African Penguin, Axolotl (one endangered, one not, one with null classification)
- **2 exhibits:** "Bamboo Grove" (open, capacity 5), "Aquatic House" (open, capacity 20)
- **2 zookeepers:** with known GUIDs (`A1B2C3D4-...` and `E5F6A7B8-...`) for deterministic assertions
- **4 animals:** spread across species/exhibits/keepers, mix of null/non-null optional fields
- **6 feeding schedules:** covers composite key, different days/time slots

### Seed SQL Dialect Differences

Each dialect requires different DDL syntax. The three seed files must account for these differences:

| Feature | SQLite | MSSQL | PostgreSQL |
|---------|--------|-------|------------|
| Auto-increment | `INTEGER PRIMARY KEY AUTOINCREMENT` | `INT IDENTITY(1,1)` | `SERIAL` or `INT GENERATED ALWAYS AS IDENTITY` |
| GUID type | `TEXT` (stored as string) | `UNIQUEIDENTIFIER` | `UUID` |
| Boolean type | `INTEGER` (0/1) | `BIT` | `BOOLEAN` |
| Date type | `TEXT` (ISO 8601) | `DATE` | `DATE` |
| DateTime type | `TEXT` (ISO 8601) | `DATETIME2` | `TIMESTAMP` |
| Decimal type | `REAL` | `DECIMAL(p,s)` | `NUMERIC(p,s)` |
| Row version | not supported (column omitted) | `ROWVERSION` | not supported (column omitted) |
| Text/long string | `TEXT` | `NVARCHAR(MAX)` | `TEXT` |

The Exhibit table's `RowVersion` column is only present in `mssql-seed.sql`. The SQLite and PostgreSQL seed files omit it entirely, and the Exhibit model class should handle its absence gracefully (the column is marked `IsRowVersion` which the ORM already skips for dialects where `SupportsRowVersion = false`).

## Test Coverage Plan

### Unit Tests

**WhereExpressionParserTests**
- Basic comparisons: `==`, `!=`, `<`, `<=`, `>`, `>=` with int, string, decimal, DateTime
- Null comparisons: `== null` to `IS NULL`, `!= null` to `IS NOT NULL`
- ANSI null compensation on/off
- Boolean fields: `x => x.IsOpen`, `x => !x.IsEndangered`
- Logical operators: `&&`, `||`, nested combinations with correct parenthesization
- `SqlIn()` with int arrays, string arrays, GUID arrays, empty arrays (verify `ArgumentException` thrown for empty)
- String methods: `Contains`, `StartsWith`, `EndsWith` to LIKE patterns
- Arithmetic: `x => x.Capacity + 5 > 10`
- GUID literal comparisons
- Parameterization: literals become parameters, verify parameter values
- Dialect-specific output: SqlServer, PostgreSQL, MySQL, SQLite, Generic
- NoLock flag, UseQuotedIdentifier flag, UseTableWithFieldNames flag

**SqlFilterTests**
- Each operator: EQ, NEQ, GT, GTE, LT, LTE, NGT, NLT
- Null filters: NULL(), NOTNULL()
- Pattern filters: LIKE, NOTLIKE, ILIKE, NOTILIKE
- Regex filters: MATCH, NOTMATCH, IMATCH, NOTIMATCH
- Parameter generation and indexing
- ToString() output per dialect
- Combining multiple filters
- Field name quoting per dialect

**DataMapTests**
- Map generation from attributed class (zoo models)
- Field discovery: readable, writable, key fields, partition keys
- Implicit field definition
- Key field identification (single, composite, GUID)
- Field lookup by name and PropertyInfo
- AddField() / RemoveField() dynamic modification
- RowVersion field detection, nullable field tracking
- Schema versioning
- Caching: second call returns same instance

**DataClassTests** (change tracking)
- New object starts as Added
- After CommitValues() becomes Unchanged
- Setting a field transitions to Modified, field appears in OriginalValues
- Multiple field changes tracked independently
- CommitValues() clears OriginalValues, resets to Unchanged
- OnBeforeSave / OnAfterSave hooks fire

**CommandBuilderTests**
- SELECT with all readable fields
- SELECT with WHERE clause (string filter, SqlFilter array)
- SELECT with range/pagination
- INSERT with auto-increment field excluded
- INSERT + select-back variants (None, IdentityOrVersion, AllFields)
- UPDATE with ChangedFields vs AllFields
- UPDATE with KeyAndVersion criteria (row version in WHERE)
- DELETE by key, DELETE with filter
- Quoted identifiers on/off
- Table name with schema prefix
- Test against multiple dialects

**DialectTests**
- FormatFieldName() quoting per dialect
- FormatTableName() with schema
- FormatParameterName() per dialect
- FormatAutoIncrementSelect() per dialect
- FormatLimitQuery() pagination per dialect
- FormatUnaryBoolean()
- Feature flags: SupportsRowVersion, SupportsSchema, SupportsNoLock, etc.
- ParseWhereFunction() for string methods per dialect

### Integration Tests (Generic Base Classes)

**CrudTests\<TFixture\>**
- Insert new Animal, verify auto-increment ID
- Insert Zookeeper with explicit GUID key
- GetSingleItem by int key, GUID key, composite key
- Update single field with ChangedFields, verify only that column updated
- Update with AllFields, verify all columns written
- Save new (insert) then save again (update)
- Delete by key, verify deleted
- Row version concurrency conflict (Exhibit) — skip when `Db.SupportsRowVersion == false`
- TrySave returns result instead of throwing
- Null field handling: insert with null, update to non-null, update back to null

**FillTests\<TFixture\>**
- FillAll() returns all seeded animals
- Fill with LINQ expression, SqlFilter, string filter
- Fill with boolean filter, null filter, compound filter
- FillRange with paging (skip/take)
- GetCount with filter
- Exists with matching and non-matching filter
- Fill with sort order

**TransactionTests\<TFixture\>**
- Insert in transaction, commit, row exists
- Insert in transaction, rollback, row gone
- WithTransaction() successful lambda, committed
- WithTransaction() exception in lambda, rolled back
- Multiple operations in single transaction

**BulkOperationTests\<TFixture\>**
- BulkInsert multiple animals, verify all inserted
- BulkUpdate, verify modifications applied

## Docker Configuration

### docker-compose.yml

```yaml
services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Zonkey#Test123"
    ports:
      - "1433:1433"

  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: zonkey
      POSTGRES_PASSWORD: zonkey
      POSTGRES_DB: zonkey_test
    ports:
      - "5432:5432"
```

Developer workflow: `docker-compose up -d` then `dotnet test`.

## CI Configuration

### GitHub Actions: build-and-test.yml

Two jobs triggered on push to master and pull requests to master.

**core-tests job** (unit tests + SQLite integration — no external services needed)**:

| Runner | Target Frameworks | Databases |
|--------|-------------------|-----------|
| windows-latest | net48 + net10.0 | SQLite only (others skip) |
| ubuntu-latest | net10.0 | SQLite only (others skip) |

Windows runner validates net48 compilation and execution (the only place net48 is tested). Ubuntu runner validates Linux compatibility on net10.0. MSSQL/PostgreSQL tests skip gracefully on both runners since those databases aren't available in this job.

**integration-tests job:**

| Runner | Target Frameworks | Databases |
|--------|-------------------|-----------|
| ubuntu-latest | net10.0 | SQLite + MSSQL + PostgreSQL |

Uses GitHub Actions service containers for MSSQL 2022 and PostgreSQL 17 with health checks. Environment variables point tests at the service containers.

### Existing release workflow

The existing `build-packages.yml` workflow for NuGet packaging on release remains unchanged.

## Project Structure

```
test/Zonkey.Tests/
    Zonkey.Tests.csproj

    Infrastructure/
        IDatabaseFixture.cs
        SqliteFixture.cs
        MssqlFixture.cs
        PgsqlFixture.cs
        TestConfiguration.cs

    Models/
        Animal.cs
        Species.cs
        Exhibit.cs
        Zookeeper.cs
        FeedingSchedule.cs

    Seed/
        sqlite-seed.sql
        mssql-seed.sql
        pgsql-seed.sql

    Unit/
        WhereExpressionParserTests.cs
        SqlFilterTests.cs
        DataMapTests.cs
        DataClassTests.cs
        CommandBuilderTests.cs
        DialectTests.cs

    Integration/
        CrudTests.cs
        FillTests.cs
        TransactionTests.cs
        BulkOperationTests.cs

        Sqlite/
            SqliteCrudTests.cs
            SqliteFillTests.cs
            SqliteTransactionTests.cs
            SqliteBulkOperationTests.cs

        Mssql/
            MssqlCrudTests.cs
            MssqlFillTests.cs
            MssqlTransactionTests.cs
            MssqlBulkOperationTests.cs

        Pgsql/
            PgsqlCrudTests.cs
            PgsqlFillTests.cs
            PgsqlTransactionTests.cs
            PgsqlBulkOperationTests.cs

docker-compose.yml
.github/workflows/build-and-test.yml
```

## What Gets Deleted

- `test/UnitTests.Core/` — entire existing test project and all contents
- All 73 AdventureWorks model classes
- Hardcoded connection strings to developer machines

## What Stays Untouched

- All source code in `src/`
- Existing `build-packages.yml` CI workflow
- `Zonkey.Mocks` project (still shipped as NuGet package)

## Dependencies

### NuGet Packages for Test Project

- `xunit.v3` (v3.x — native `Assert.Skip()` support, `IAsyncDisposable` fixtures)
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `Microsoft.Data.Sqlite`
- `Npgsql`
- `Microsoft.Data.SqlClient`

### Project References

- `Zonkey.Data`
- `Zonkey.Data.MsSql`

## Required Source Changes

These are minimal changes to `src/Zonkey.Data` needed to support the test infrastructure:

1. **Register `Microsoft.Data.Sqlite` in `SqlDialect.Factories`**: The current factory dictionary only registers `Mono.Data.Sqlite.SqliteConnection`. The `Microsoft.Data.Sqlite.SqliteConnection` type name must be added so that `SqlDialect.Create()` returns `SqliteDialect` when given a `Microsoft.Data.Sqlite` connection. This is a one-line addition to the static dictionary in `SqlDialect.cs`.

   Note: The fixtures manually instantiate dialects as a safety net, but fixing the factory registration is the right long-term fix since application code also benefits.

## Planned Follow-ups

1. **PostgreSQL `xmin` row version support**: Add `SupportsRowVersion = true` to `PostgreSqlDialect` using PostgreSQL's system `xmin` column for optimistic concurrency. This requires changes to both the dialect and the command builder to handle `xmin` as a row version field. Once implemented, enable row version concurrency tests on the PostgreSQL fixture.

## Open Questions

None — all design decisions have been resolved during brainstorming.
