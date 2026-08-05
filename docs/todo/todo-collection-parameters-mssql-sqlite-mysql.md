# TODO: Collection Parameters for IN-lists — SQL Server, SQLite, MySQL

**Status:** Designed 2026-07-25, PostgreSQL implemented same day; remaining providers deliberately deferred. This doc captures the design so the work can be picked up cold.

## Problem

On dialects with no collection parameter, `list.Contains(field)` translation sends one parameter per
element (2-64 items), inlines literals for safe types (>64), or throws past the dialect's
`MaxParameters`. (A single value collapses to `=`, and PostgreSQL — which has the collection path —
uses it from two values up.) Large, *varying* lists on the remaining dialects cause:

1. **Plan-cache pollution** — each distinct parameter count is distinct query text (one cached plan per
   count); inlined literals are one plan per distinct value set. On hot paths this bloats the cache and
   burns CPU on recompiles. This is the failure mode that drove EF Core 8's redesign of the same feature.
2. **Compile cost** — thousands-element IN lists take real parse/plan time.
3. **Hard parameter walls** — `SqlDialect.MaxParameters` (SQL Server 2100, SQLite 32766, MySQL/PG 65535).

A single *collection-valued* parameter fixes all three: one stable query text, one plan, one parameter.

## What already exists (PostgreSQL, shipped)

- `SqlDialect.MaxParameters` — virtual, per-dialect (`SqlDialect.cs`).
- `SqlDialect.SupportsInCollectionParameter(Type elementType)` — base `false`.
- `SqlDialect.RenderInCollectionParameter(string operand, string placeholder)` — base throws.
- `SqlTextGenerator.VisitInValues` — when `ParameterizeLiterals` && `values.Count > InlineThreshold (64)`
  && the dialect supports the element type: builds a **typed array** via `Array.CreateInstance`
  (element type = `values[0].GetType()`), adds it as ONE parameter, renders via the dialect hook.
  Order rule: the operand renders *before* the array parameter is numbered, so operand-embedded
  parameters keep their positions.
- `PostgreSqlDialect`: `(operand = ANY($n))`. Element-type support is an *exclusion* list, not "everything
  except byte" — Npgsql doesn't map every CLR scalar to an array form: `byte` (binds as `bytea`, not
  `smallint[]`), `sbyte`/`ushort`/`uint`/`ulong` (no PG array mapping at all — these types worked before
  only via literal inlining and would fail at bind time as arrays), and enums (a scalar enum parameter is
  converted by `FixParameter`, but that hook never runs for array elements, and Npgsql only binds arrays
  for explicitly mapped native enums, which the dialect cannot detect — enum lists stay individually
  parameterized). Remaining scalar-mappable types are supported — `int[]→integer[]`, `long[]→bigint[]`,
  `string[]→text[]`, `Guid[]→uuid[]` — with one caveat: `DateTime` arrays require a consistent
  `DateTimeKind` across all elements (the same rule Npgsql applies to scalar `DateTime` parameters,
  enforced array-wide here since one bad element fails the whole bind).
- Policy (revised 2026-08-03): a single value collapses to `=` everywhere. Above that, a dialect WITH a
  collection parameter uses it from 2 elements up — one plan at any size, so there is no threshold to
  tune. Dialects WITHOUT one keep individual parameters to 64, then inline safe literals; the 64 bound
  exists there to cap plan-cache churn (one plan per arity, reused) before trading it for the ability
  to exceed the parameter cap. When implementing OPENJSON/json_each/JSON_TABLE below, those dialects
  should move to the ">1 uses the collection parameter" rule too, and `InlineThreshold` becomes dead.
  Legacy
  `SqlInInt`/`SqlInGuid` (`SqlInValuesInline`) are untouched — always literal-inlined.
- Null semantics unaffected: nulls are stripped and `OR field IS NULL` is added by the translator
  *before* the generator chooses a strategy, so the array never contains nulls.

## Design for the remaining providers

### Shape the hooks around a collection-parameter descriptor, not `values[0].GetType()`

The PG implementation gets away with deriving everything from the CLR runtime type of the first element
because `= ANY(array)` needs nothing else. JSON/TVP paths do — the OPENJSON `CAST` target, the TVP column
type, and (for `decimal`/`string`) the precision/length all have to come from **mapped field metadata**,
not a fixed guess (`nvarchar(4000)`, `decimal(38,10)`) or the runtime CLR type alone (a CLR `decimal` with
no attribute doesn't carry the DB's actual scale). Before implementing the JSON/TVP tiers, reshape:

- Thread a small descriptor through the collection-parameter path instead of a bare `Type`:
  `readonly struct CollectionParameterInfo { Type ElementType; DbType? DbType; int? Precision; int?
  Scale; int? Length; }`, populated from the `IDataMapField` for the operand column when one is known
  (falls back to CLR-type-only inference for expressions without a mapped column, matching today's PG
  behavior).
- `RenderInCollectionParameter` and any value-shaping hook (`BuildInCollectionParameterValue`,
  `CreateInCollectionParameter`) should receive this descriptor, not just `values[0].GetType()`, so a
  dialect can pick `nvarchar(255)` vs `nvarchar(4000)` or `decimal(18,4)` vs `decimal(38,10)` correctly
  instead of guessing wide and hoping.

### SQL Server — two tiers

**Tier 1 (default, no DDL): OPENJSON.** SQL Server 2016+.

```sql
WHERE [SpeciesId] IN (SELECT CAST(value AS int) FROM OPENJSON(@p0))
```

with `@p0 = '[1,2,3]'` (nvarchar). This is EF Core 8's translation. Needs:

- A value-shaping hook the PG path didn't need: the parameter value is a JSON **string**, not the array.
  Proposed: `virtual object BuildInCollectionParameterValue(Array values)` on `SqlDialect`
  (base: return the array unchanged; SqlServer: serialize to JSON). Serialization must correctly handle
  escaping, surrogate pairs, and control characters in strings, plus stable decimal/DateTime formatting —
  this is not a ~30-line hand-rolled job to get right. Use `System.Text.Json` where it's available (net8+
  targets); for net48 (no STJ dependency by default) either ship a small, exhaustively-tested serializer
  covering just the supported element types, or scope JSON collection-parameter support to net8+ builds
  only and leave net48 on the existing per-element/inline behavior.
- The CAST target type must come from **mapped field metadata** (DbType, precision/length from the
  `DataMap` field), not a fixed guess like `nvarchar(4000)` or `decimal(38,10)` — see the descriptor
  reshape below.
- **Version gate**: OPENJSON needs database compatibility level 130+, which a server binary version alone
  does not guarantee — a SQL Server 2022 instance can still host a database restored at compat level 110.
  Do not default this to on with a settable-false escape hatch (`UseJsonCollectionParameters = true`) —
  a fresh server pointed at a legacy-compat-level database would silently break. Instead: require either
  explicit opt-in (`UseJsonCollectionParameters` defaults `false`, consumer turns it on after confirming
  compat level), or a detected-and-cached capability (query
  `sys.databases.compatibility_level` once per connection/dialect instance and cache the result) so the
  gate reflects the actual database, not an assumption about the server binary.

**Tier 2 (opt-in): TVP.**

```sql
WHERE [SpeciesId] IN (SELECT [Value] FROM @p0)
```

with a `SqlDbType.Structured` parameter streaming `SqlDataRecord`s. Not framed as a "premium" tier over
Tier 1 — TVPs still carry weak cardinality estimates (same caveat as OPENJSON, see below) and add
deployment burden; the tradeoff is a different shape, not strictly better. Requirements and placement:

- Needs a user-defined table type in the target database (`CREATE TYPE dbo.IntList AS TABLE (Value int)`).
  Zonkey does not manage schema → strictly opt-in via configuration mapping element type → type name.
  The UDTT name must be validated/quoted like any other identifier the dialect emits (do not
  string-concatenate a caller-supplied type name unquoted into SQL). Creating the type, and granting
  `EXECUTE`/`REFERENCES` permissions on it, are the consumer's responsibility — Zonkey only binds against
  a type it assumes already exists.
- `SqlParameter`/`SqlDataRecord` are `Microsoft.Data.SqlClient` types → implementation lives in
  **Zonkey.Data.MsSql** (a `SqlServerDialect` subclass registered through `SqlDialect.Factories`, the
  same mechanism `MsSqlExtension` already uses), NOT in core.
- Value shaping: `FixParameter(DbParameter)` is `void` — it can mutate the parameter passed in, but it
  cannot swap in a different `DbParameter` instance, which is what a Structured/TVP parameter needs
  (`SqlParameter` with `SqlDbType.Structured` and a `SqlDataRecord` stream has to be the parameter
  instance itself, not a mutation of a plain one). Replace that mechanism with a parameter-**creation**
  hook — e.g. `virtual DbParameter CreateInCollectionParameter(DbCommand command, string name, Array
  values, ElementInfo element)` — that the dialect/extension implements and returns the final parameter
  from. `DataManager.AddIndexedParameter` (currently `command.CreateParameter()` + set `.Value`) needs to
  call this hook instead of assuming a single creation path, so TVP dialects can hand back a fully-formed
  Structured parameter rather than have one mutated after the fact.

### SQLite — json_each

```sql
WHERE "SpeciesId" IN (SELECT value FROM json_each(@p0))
```

`json_each` ships in the e_sqlite3 bundle used by Microsoft.Data.Sqlite. Same JSON-string value shaping
as SQL Server Tier 1. SQLite is dynamically typed, so no CAST needed for integers/text; GUIDs stored as
TEXT compare fine when serialized uppercase-consistent with storage format (verify against
Zonkey's SQLite Guid storage convention before enabling — this is the one open question; if Guids are
stored as BLOB, exclude `Guid` from `SupportsInCollectionParameter` here).

### MySQL — JSON_TABLE (8.0.4+, MariaDB 10.6+)

```sql
WHERE Name IN (SELECT v FROM JSON_TABLE(@p0, '$[*]' COLUMNS (v INT PATH '$')) AS zk_list)
```

Same JSON value shaping; column type from mapped field metadata like SQL Server. `JSON_TABLE` arrived in
**MySQL 8.0.4** (not "8.0+" generally — pre-8.0.4 8.x releases don't have it), and **MariaDB does not
support `JSON_TABLE` until 10.6**. `MySqlDialect` in this codebase serves both MySQL and MariaDB
connection strings, so the gate cannot be a single MySQL-version flag — it needs to branch on server
family (detectable from `SELECT VERSION()`, which returns a MariaDB-flavored string on MariaDB) and apply
the correct minimum per family, in addition to whatever opt-in/detected-capability mechanism SQL Server
Tier 1 lands on. `FIND_IN_SET` was considered and rejected (no index use, string-typed comparison).

### Oracle / DB2 / Access

Out of scope (best-effort tier, no test coverage). Oracle has collection binding
(`MEMBER OF` / `TABLE(:1)` with ODP.NET associative arrays) but it is provider-specific and untestable
here today. These dialects keep the current inline/parameterize+cap behavior.

## Caveat to preserve in all implementations (from EF's experience)

OPENJSON/json_each/JSON_TABLE (and TVPs) have **weak cardinality estimates** — the optimizer guesses a
fixed row count instead of seeing values. Harmless for simple IN filters; can regress plans for selective
joins. EF Core 9 added an escape hatch back to constants for exactly this reason. Therefore:

- Reconsider the ">1 → collection parameter" rule per dialect if estimates regress; the fallback is the
  individual-parameter form, which is what PostgreSQL used below 64 before 2026-08-03.
- `CollectionParameterMode` (`Auto | Never | Always`) needs two levels, not just adapter-level: an
  adapter/dialect-level default plus a **query-level override** (e.g. a parameter on the relevant
  `Fill`/adapter-call overload, or a fluent option on the expression-filter API) so a single regressed
  query can opt out without changing the default for every other query against that adapter.
  `Always` needs a documented fallback rule rather than an implicit "trust the caller": for ≤64-element
  lists, or lists whose element type the dialect doesn't support as a collection parameter, `Always` still
  falls back to the individual-parameter/inline path — it must never throw just because the caller asked
  for a mode the data doesn't fit. `Auto` is the ≤64-individual / >64-collection policy already shipped
  for PG; `Never` always uses the pre-collection-parameter behavior regardless of size.

### EF's compatibility lessons

EF Core's rollout of the same idea is worth mirroring, including the parts that went wrong. EF Core 8
shipped OPENJSON-based parameterized collections **on by default**; because OPENJSON's weak cardinality
estimates could regress plans that used to get exact per-value estimates, this caused real production
regressions, and EF Core 8 patch releases added a compatibility-level escape hatch
(`TranslateParameterizedCollectionsToConstants`-style opt-out) after the fact. EF Core 9 went further and
added both global translation controls and **per-query overrides**, learning that a single global switch
wasn't granular enough. Zonkey should not repeat the "ship default-on, patch in the escape hatch later"
sequence: ship each provider's collection-parameter support **capability-gated** (detected/opt-in per the
SQL Server and MySQL sections above) **with the `Auto`/`Never`/`Always` override already in place from day
one**, not added reactively once someone hits a regressed plan in production.

## Test plan when picking this up

- Unit golden per dialect per element type (int/long/string/Guid/decimal/DateTime), boundary 64/65,
  null-in-list composition (`(... ) OR (x IS NULL)` wraps the collection form — already covered for PG).
- Integration: MSSQL OPENJSON path against the docker container (2016+ image — current compose uses a
  modern tag, verify); SQLite json_each in the always-on suite; MySQL has no container in compose today —
  unit goldens only until one is added.
- JSON-injection check: string values containing `"`, `\`, control chars round-trip through the JSON
  serializer correctly (unit + integration with hostile strings).
- A TVP end-to-end test needs the UDTT created in the MSSQL seed script (`test/Zonkey.Tests/Seed/`).
- SQLite test plan needs more than int/text coverage given dynamic typing: verify **type affinity**
  behavior for the JSON-sourced collection path specifically — numeric affinity for
  int/long/decimal-as-numeric-string round-trips, bool storage (0/1 vs `json_each`'s native JSON
  true/false), decimal precision surviving the JSON string round-trip, `DateTime` formatting matching
  Zonkey's existing SQLite date storage convention, and GUIDs specifically against **both** of Zonkey's
  SQLite GUID storage conventions (TEXT vs BLOB — see the open question in the SQLite section above) so
  the json_each comparison is verified against whichever convention is actually enabled, not assumed.

## Pointers

- Generator strategy branch: `SqlTextGenerator.VisitInValues` (`src/Zonkey.Data/ObjectModel/QueryTranslation/SqlTextGenerator.cs`)
- Dialect hooks: `SqlDialect.SupportsInCollectionParameter` / `RenderInCollectionParameter` / `MaxParameters`
- PG reference implementation: `PostgreSqlDialect.cs`
- Parameter materialization (where TVP replacement hooks in): `DataManager.AddIndexedParameter` (`DataManager.cs`)
- Prior art: EF Core 8 "queryable primitive collections" (OPENJSON), Npgsql EF `= ANY` translation,
  EF Core 9 `TranslateParameterizedCollectionsToConstants` escape hatch.
