# Test Reengineering — Execution Results

**Plan:** `docs/superpowers/plans/2026-03-11-test-reengineering.md`
**Spec:** `docs/superpowers/specs/2026-03-11-test-reengineering-design.md`
**Executed:** 2026-03-12

---

## Final Test Counts

| Framework | Passed | Skipped | Failed | Total |
|-----------|--------|---------|--------|-------|
| net10.0   | 113    | 47      | 0      | 160   |
| net48     | 82     | 0       | 0      | 82    |

**net10.0 skips:** 46 MSSQL/PostgreSQL integration tests (no Docker running locally) + 1 SQLite RowVersion test (unsupported by provider).

**net48:** All integration tests and CommandBuilderTests are behind `#if !NETFRAMEWORK` — only unit tests run.

---

## Tasks Completed

### Chunk 1: Project Scaffolding (Tasks 1–4)
- Created `test/Zonkey.Tests/Zonkey.Tests.csproj` targeting `net10.0;net48`
- xUnit v3 (1.1.0) with `OutputType=Exe` and `LangVersion=latest`
- DB packages (Microsoft.Data.Sqlite, Npgsql, Microsoft.Data.SqlClient) conditional on `!net48`
- Created `global.json` to pin .NET 10 SDK (avoids workload manifest conflict)
- Created 5 zoo-themed models: Animal, Species, Exhibit, Zookeeper, FeedingSchedule
- Created seed SQL for SQLite, MSSQL, and PostgreSQL
- Created `docker-compose.yml` for MSSQL 2022 + PostgreSQL 17

### Chunk 2: Database Fixtures (Tasks 5–7)
- `IDatabaseFixture` interface with `IAsyncLifetime`, `IsAvailable`, `SupportsRowVersion`
- `TestConfiguration` with env var overrides (`ZONKEY_TEST_MSSQL`, `ZONKEY_TEST_PGSQL`)
- `SqliteFixture` — temp file DB, seeds from `sqlite-seed.sql`, deletes on dispose
- `MssqlFixture` — creates unique DB on master, seeds via `Regex.Split` on `GO`, drops on dispose
- `PgsqlFixture` — creates unique DB, seeds, terminates connections and drops on dispose
- All fixture classes wrapped in `#if !NETFRAMEWORK`

### Chunk 3: Unit Tests (Tasks 8–13)

| Test Class | Tests | Notes |
|------------|-------|-------|
| DataClassTests | 8 | Change tracking, DataRowState transitions, OriginalValues |
| DataMapTests | 14 | Reflection cache, field discovery, keys, auto-increment, caching |
| SqlFilterTests | 14 | EQ, NEQ, GT, GTE, LT, LTE, NULL, NOTNULL, LIKE, NOTLIKE, params |
| DialectTests | 16 | SqlServer, Sqlite, PostgreSql, MySql feature flags and formatting |
| WhereExpressionParserTests | 22 | Comparisons, null, boolean, logical, SqlIn, string methods, arithmetic |
| CommandBuilderTests | 9 | SELECT, INSERT, DELETE commands; composite keys; dialect quoting |

### Chunk 4: Integration Tests (Tasks 14–17)

| Base Class | Tests | Coverage |
|------------|-------|----------|
| CrudTests | 10 | Insert (auto-increment + GUID key), GetOne (int/GUID/composite key), Update, SaveNew→Update, Delete, null field round-trip, RowVersion concurrency |
| FillTests | 10 | FillAll, Fill with LINQ/SqlFilter/string/null/boolean/compound filters, GetCount, GetCount matching/non-matching |
| TransactionTests | 2 | Commit persists, Rollback discards |
| BulkOperationTests | 1 | BulkInsert multiple records |

Each base class has 3 concrete subclasses: `Sqlite/`, `Mssql/`, `Pgsql/`.

### Chunk 5: CI & Cleanup (Tasks 18–20)

- Created `.github/workflows/build-and-test.yml` with two jobs:
  - `core-tests` — matrix (windows-latest, ubuntu-latest)
  - `integration-tests` — ubuntu with MSSQL + PostgreSQL service containers
- Added `Zonkey.Tests` to `Zonkey.sln`
- **NOT done:** Old test project (`test/UnitTests.Core/`) not deleted (git read-only)

---

## Deviations from Plan

### Source Changes Required

| Change | Reason |
|--------|--------|
| Made `WhereExpressionParser`, `WhereExpressionParser<T>`, `SqlWhereClause` public | Strong-named assembly blocked `InternalsVisibleTo` without public key. Making classes public was simpler than managing keys. |
| Made `Parse(LambdaExpression, ArrayList)` overload public | Needed by parameterization unit test |

### Test Adjustments

| Planned | Actual | Reason |
|---------|--------|--------|
| `adapter.Exists(expr)` | `adapter.GetCount(expr) > 0` | `Exists()` generates T-SQL `IF EXISTS(...)` syntax — SQLite doesn't support `IF` |
| `Guid.Parse("...")` inside lambda | Extract to variable, use variable in lambda | `WhereExpressionParser` can't evaluate method calls inside expression trees |
| `Assert.Equal(2, count)` for GetCount | `Assert.Equal(2L, count)` | `GetCount()` returns `long`, not `int` |
| `Assert.Contains("<>", ...)` for NEQ | `Assert.Contains("!=", ...)` | Parser generates `!=`, not `<>` |
| `GenericSqlDialect` for string method tests | `SqlServerDialect` | `GenericSqlDialect.ParseWhereFunction` throws `NotImplementedException` |
| `SqlFilter.NULL().Value == null` | `== DBNull.Value` | `NULL()` stores `DBNull.Value`, not `null` |
| `SqlServer_UsesAtParameters` test | `SqlServer_OutputContainsFieldName` | Parser uses `$` prefix for literal placeholders, not `@` |

### Skipped Work

- **Old test project deletion** (`test/UnitTests.Core/`) — git was read-only. Run manually:
  ```bash
  dotnet sln Zonkey.sln remove test/UnitTests.Core/UnitTests.csproj
  rm -rf test/UnitTests.Core
  ```
- **Strong-name key removal** — User said strong naming can be dropped but this wasn't acted on. `SignAssembly` and `.snk` references remain in csproj files.

---

## Follow-up Session (2026-07-18) — Completion

The 2026-03-12 run never exercised the MSSQL/PostgreSQL integration tests (no Docker at the time) and left work uncommitted. A follow-up session verified everything live and finished the remaining items:

**Verified:** `net10.0` 158 passed / 2 skipped (by-design RowVersion skips on SQLite + PostgreSQL) / 0 failed against live MSSQL 2022 and PostgreSQL 17 containers; `net48` 82 passed. Full solution build also verified on Linux (`mcr.microsoft.com/dotnet/sdk:10.0` container) matching the CI jobs.

**Fixes made:**

| Change | Reason |
|--------|--------|
| `Seed/pgsql-seed.sql` rewritten with unquoted identifiers | PostgreSQL folds unquoted identifiers to lowercase; the quoted-PascalCase schema never matched Zonkey's unquoted SQL — all 22 PG tests failed with `relation "animal" does not exist` on first live run. Zonkey's field lookup is case-insensitive, so lowercase columns round-trip fine. |
| `docker-compose.yml` host ports → 1434 (MSSQL), 5433 (PG); `TestConfiguration` defaults updated to match | Dev machines commonly have a local SQL Server on 1433 / PostgreSQL on 5432; the old mapping silently lost the port race and tests hit the wrong server. CI is unaffected (sets env vars explicitly). |
| Added `Directory.Build.props` with `Microsoft.NETFramework.ReferenceAssemblies` for net48 | `dotnet build Zonkey.sln` on the ubuntu CI runners failed with MSB3644 (no .NET Framework reference assemblies on Linux). |
| `global.json` → version 10.0.100, `rollForward: latestFeature` | The old `10.0.200` + `latestPatch` pin refused the installed 10.0.400-preview SDK. |
| Added direct `SQLitePCLRaw.bundle_e_sqlite3 2.1.*` reference to Zonkey.Tests | Lifts transitive SQLitePCLRaw.lib.e_sqlite3 2.1.10 above known vulnerability GHSA-2m69-gcr7-jv3q (NU1903 warning). |
| Fixed version-extraction step in `build-packages.yml` | `echo "VERSION=..." -replace '^v',''` was invalid PowerShell and would not have stripped the `v` prefix. |
| `pg_isready -U zonkey` in `build-and-test.yml` healthcheck | Parity with docker-compose healthcheck. |
| Removed `test/UnitTests.Core` and its solution entry | The deferred deletion from the original run. |
| Updated `CLAUDE.md` | Build/test commands still referenced the deleted MSTest project and pre-migration target frameworks. |

**Also completed (user-approved):** strong-name signing dropped from all src projects (`SignAssembly`/`.snk` removed). With signing gone, `InternalsVisibleTo("Zonkey.Tests")` works without a public key, so the March workaround that made `WhereExpressionParser`, `WhereExpressionParser<T>`, and `SqlWhereClause` public was reverted — they are internal again and the tests use `InternalsVisibleTo`.

## Known Limitations

1. **`Exists()` is SQL Server-only** — generates `IF EXISTS(...)` which is T-SQL. Won't work on SQLite, PostgreSQL, or MySQL. Library bug, not test bug.
2. **RowVersion only works on MSSQL** — SQLite and PostgreSQL fixtures set `SupportsRowVersion = false`. The concurrency test skips on those providers.
3. **`WhereExpressionParser` can't evaluate method calls** — `Guid.Parse()`, `DateTime.Now`, etc. inside lambdas will throw `NotSupportedException`. Values must be extracted to variables first.

---

## File Inventory

```
test/Zonkey.Tests/
├── Zonkey.Tests.csproj
├── Infrastructure/
│   ├── IDatabaseFixture.cs
│   ├── TestConfiguration.cs
│   ├── SqliteFixture.cs
│   ├── MssqlFixture.cs
│   └── PgsqlFixture.cs
├── Models/
│   ├── Animal.cs
│   ├── Species.cs
│   ├── Exhibit.cs
│   ├── Zookeeper.cs
│   └── FeedingSchedule.cs
├── Seed/
│   ├── sqlite-seed.sql
│   ├── mssql-seed.sql
│   └── pgsql-seed.sql
├── Unit/
│   ├── DataClassTests.cs
│   ├── DataMapTests.cs
│   ├── SqlFilterTests.cs
│   ├── DialectTests.cs
│   ├── WhereExpressionParserTests.cs
│   └── CommandBuilderTests.cs
└── Integration/
    ├── CrudTests.cs
    ├── FillTests.cs
    ├── TransactionTests.cs
    ├── BulkOperationTests.cs
    ├── Sqlite/
    │   ├── SqliteCrudTests.cs
    │   ├── SqliteFillTests.cs
    │   ├── SqliteTransactionTests.cs
    │   └── SqliteBulkOperationTests.cs
    ├── Mssql/
    │   ├── MssqlCrudTests.cs
    │   ├── MssqlFillTests.cs
    │   ├── MssqlTransactionTests.cs
    │   └── MssqlBulkOperationTests.cs
    └── Pgsql/
        ├── PgsqlCrudTests.cs
        ├── PgsqlFillTests.cs
        ├── PgsqlTransactionTests.cs
        └── PgsqlBulkOperationTests.cs

Other files:
├── .github/workflows/build-and-test.yml
├── docker-compose.yml
└── global.json
```
