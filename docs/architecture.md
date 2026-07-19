# Architecture -- The Life of a Query and a Save

This document connects the pieces. The other docs describe each component on its own; this one traces two complete operations -- a `Fill` and a `Save` -- through every layer, and explains *why* each layer exists. Read this if you are contributing to Zonkey, debugging generated SQL, or want to understand what actually happens between your method call and the database.

## The Component Map

| Component | Source | Responsibility |
|---|---|---|
| `DataClass` + attributes | `ObjectModel/DataClass.cs`, `DataFieldAttribute.cs`, `DataItemAttribute.cs` | Declare the mapping; track per-object state |
| `DataMap` | `ObjectModel/DataMap.cs` | Reflection cache: the analyzed, categorized field mappings for a type |
| `ClassFactory` | `ObjectModel/ClassFactory.cs` | IL-emitted object construction for row materialization |
| `DataClassAdapter<T>` | `DataClassAdapter/*.cs` | Orchestrates CRUD: one file per operation family |
| `DataClassCommandBuilder` | `ObjectModel/DataClassCommandBuilder/*.cs` | Generates SELECT/INSERT/UPDATE/DELETE command text and parameters |
| `SqlDialect` | `Dialects/*.cs` | Every engine-specific decision, in one class per engine |
| `WhereExpressionParser` | `ObjectModel/WhereExpressionParser.cs` | Lambda expression tree → SQL WHERE clause |
| `DataClassReader<T>` | `ObjectModel/DataClassReader.cs` | Streams a `DbDataReader` into objects |
| `DatabaseWrapper` | `ObjectModel/DatabaseWrapper.cs` | Application-facing entry point: owns the connection, caches adapters |

The layering rule: **adapters orchestrate, builders generate, dialects vary.** An adapter never writes SQL text itself; a builder never makes an engine-specific decision itself; a dialect never executes anything.

## The Life of a Fill

What happens during:

```csharp
var adapter = new DataClassAdapter<Product>(connection);
await adapter.Fill(products, p => p.Price < 25.00m);
```

### 1. The map is built (once per process)

The adapter constructor calls `DataMap.GenerateCached(typeof(T), ...)`. The cache key is `TypeFullName|tableName|keyFields|schemaVersion`, so the reflection work -- enumerating properties, reading attributes, categorizing fields into readable/writable/key/partition-key sets -- happens once per distinct mapping, ever. Everything downstream consumes the map; nothing re-reflects.

A design detail with consequences: `DataFieldAttribute` *implements* `IDataMapField`, and the map stores the attribute instances themselves rather than copying them into parallel objects. This works because reflection materializes fresh attribute instances on every `GetCustomAttribute` call -- each generated map owns its own copies. It also means mutating a cached map changes behavior process-wide, while a `GenerateNew` map is isolated. See [Data Classes](data-classes.md#datamap-advanced).

### 2. The dialect is chosen from the connection

Assigning `Connection` (the constructor does it) runs `SqlDialect.Create(connection)`, which looks up the connection's *type full name* -- `"Npgsql.NpgsqlConnection"`, `"Microsoft.Data.SqlClient.SqlConnection"` -- in the `SqlDialect.Factories` dictionary, falling back to `GenericSqlDialect`.

**Why by type name and not by type?** Zonkey would otherwise need a package reference to every ADO.NET provider it recognizes. String matching means the core library references no providers at all, and applications can register their own mappings (`SqlDialect.Factories["My.Provider.Connection"] = c => new MyDialect()`).

### 3. The lambda becomes a WHERE clause

`Fill(collection, expression)` constructs a `WhereExpressionParser<T>` with the map and dialect, propagating the adapter's quoting setting and `NoLock`. The parser walks the expression tree:

- Property accesses on the lambda parameter resolve through the `DataMap` to *column* names, formatted by `dialect.FormatFieldName` with the field-level-overridable quoting setting.
- Captured local variables and their members are read and become parameter *values*.
- Method calls are rejected (`NotSupportedException`) except the `SqlIn` family and dialect-translated string predicates -- the parser is a translator, not an evaluator. This is deliberate: silently evaluating arbitrary calls would blur the line between "runs in C#" and "runs in SQL", and that line is the whole point of a deterministic ORM.

The output is a `SqlWhereClause`: SQL text containing `$0`-style placeholders plus the parameter values.

### 4. The command is assembled

`FillInternal` → `PrepCommandForSelect` → `CommandBuilder.GetSelectCommand(whereText, orderBy)`. The command builder (created lazily, one per adapter, reset if the dialect changes) produces the column list from the map's `ReadableFields` -- each name through `FormatFieldName(name, field-level ?? builder-level quoting)` -- and the table name through `FormatTableName`. Then `DataManager.AddParamsToCommand` replaces each `$n` placeholder with `dialect.FormatParameterName(n)` (`@p0`, `:p0`, `?`) and creates the `DbParameter` objects.

### 5. The command executes

`ExecuteReaderInternal` applies the timeout (`CommandTimeout ?? DefaultCommandTimeout`), enrolls the command in a transaction (the adapter's explicit `Transaction` if set, otherwise whatever `DbTransactionRegistry` holds for this connection), raises the `BeforeExecuteCommand` event (cancellable), and calls the provider's native `ExecuteReaderAsync`.

### 6. Rows become objects

`PopulateCollection` wraps the reader in a `DataClassReader<T>`, which does three things once per result set:

- Builds a column-name → ordinal dictionary from the reader and pairs it against the map's readable fields (case-insensitive).
- Gets a construction delegate from `ClassFactory`, which emits a tiny `DynamicMethod` (`newobj`/`ret`) per type and caches it -- because `Fill` constructs one object per row, and `Activator.CreateInstance` reflection cost would dominate large fills. This is why mapped classes need a public parameterless constructor (or a registered factory / `adapter.ObjectFactory`).
- Materializes rows -- by default through the **fast builder**: a second `DynamicMethod`, emitted once per (type, result-set shape), containing branch-free straight-line IL per mapped column (null-check, convert, set). Because the reader's column types are known at emit time, every conversion decision (direct unbox, `Guid`-from-string, enum widening, nullable wrapping, `DateTimeKind` stamping, `Convert.ChangeType` fallback) is resolved while emitting, not per row. The generated code has no exception handling; instead it writes the current column ordinal into a tracker before each set, and the single try/catch around the row build converts any failure into a `PropertyReadException` naming the property and offending value -- the same exception the reflection path throws. Set `UseFastBuilder = false` (or the static `DataClassReader<T>.DefaultUseFastBuilder`) to fall back to per-field reflection via `FieldHandler`.
- Finally, `CommitValues()` is called on each `ISavable`, which is what lands every materialized object in the `Unchanged` state regardless of which constructor the class routed through.

## The Life of a Save

What happens during:

```csharp
var product = new Product(addingNew: true) { Name = "Classic Tee", Price = 24.99m };
await adapter.Save(product);
```

### 1. The gate and the fork

`Save` delegates to `TrySave` (Save.cs). First the gate: the object must implement `ISavable`, or `ArgumentException` -- this is the entire mechanism behind "attribute-only classes are read-only". Then `OnBeforeSave()` fires (a `DataClass` virtual -- it will not fire for hand-rolled `ISavable` implementations), and `DataRowState` picks the path:

| State | Path |
|---|---|
| `Added` | insert |
| `Modified` | update |
| `Detached` | `InvalidOperationException` ("did you forget the new record constructor?") |
| anything else | `SaveResult(Skipped)` -- this is the `false` return from `Save` |

### 2. The insert path

`TryInsert` asks the builder for `GetInsertCommands(obj, selectBack)`. The builder emits the INSERT (writable, non-auto-increment fields; `Guid.Empty` and null handling applied per field) and, unless `SelectBack.None`, a select-back statement that reads auto-increment and database-computed values using `dialect.FormatAutoIncrementSelect` (`SCOPE_IDENTITY()`, `lastval()`, `last_insert_rowid()`, ...).

Whether that is one round trip or two is a dialect decision: engines with `UseSqlBatches` (SQL Server) get one batched command; the rest get an INSERT command plus a separate select-back command executed on the same connection -- which is why identity retrieval functions that are connection-scoped still work. The select-back row is applied to the object via the same populate machinery as queries.

### 3. The update path

`TryUpdate` resolves the tri-state options first: `UpdateCriteria.Default` → the class's `[DataItem]` value, else `ChangedFields`; `KeyAndVersion` silently downgrades where the dialect lacks row-version support. The builder emits `UPDATE ... SET <affected fields> WHERE <criteria fields>` from the object's `OriginalValues` -- this is where per-object change tracking pays off: the WHERE clause can assert *what you last saw*, not just the key.

(When the options allow -- affect `ChangedFields`, criteria at most `ChangedFields` -- `TrySave` routes through `TryUpdate2` instead, a single-pass generator whose SET clause comes purely from `OriginalValues`. The same generator powers the [stub update pattern](data-class-adapter.md#saving----update2-and-stub-updates-update-without-loading): updating a row without ever selecting it.)

Then the rows-affected count carries the semantics:

- **1 row affected** → success; select-back (if any) refreshes the object.
- **0 rows affected** → the WHERE clause no longer matches. The select-back re-reads by key: row still there → `Conflict` (someone changed it); row gone → `Fail` (someone deleted it). This distinction is the reason conflict detection requires criteria stricter than `KeyOnly`.
- **>1 rows affected** → `Fail` (the criteria matched more than one row; `IgnoreUpdateRowCount` suppresses this check).

### 4. Commit

On success: `OnAfterSave` fires, then `CommitValues()` clears `OriginalValues` and sets `Unchanged`. The object is now indistinguishable from one freshly loaded -- which is exactly the invariant the state machine maintains: *`Unchanged` means "matches the database as far as this object knows".*

## Where the Dialect Plugs In

Every engine-specific decision funnels through one `SqlDialect` instance:

- **Text shaping**: `FormatFieldName` / `FormatTableName` (quoting -- note the deliberate family asymmetry described in [Database Providers](database-providers.md#identifier-quoting--case-sensitivity)), `FormatParameterName`, `FormatLimitQuery` (pagination), `FormatExistsQuery`, `FormatAutoIncrementSelect`, `FormatUnaryBoolean`, `ParseWhereFunction` (string-method translation)
- **Command tweaking**: `OptimizeSelectSingleCommand` (TOP 1 / LIMIT 1), `FixParameter` (e.g., SQLite Guid→string, PostgreSQL enum coercion), change-tracking context injection
- **Capability flags**: `SupportsLimit`, `SupportsRowVersion`, `SupportsNoLock`, `SupportsSchema`, `UseSqlBatches`, `UseNamedParameters` -- consulted by adapters and builders to choose strategies rather than emit broken SQL

Adding engine support means one new dialect subclass and one `Factories` entry -- no adapter or builder changes. That single-point-of-variation is the payoff of the layering rule above.

## The Caching Layers

Zonkey trades a little startup cost for zero steady-state reflection. Know these caches when reasoning about behavior:

| Cache | Scope | Key | Invalidation |
|---|---|---|---|
| `DataMap._mapCache` | static, process-wide | type + table + keys + schema version | never -- mutations are visible everywhere |
| `ClassFactory` type registry | static, process-wide | type | never (explicit `RegisterType` overrides) |
| `DataClassCommandBuilder` | per adapter, lazy | -- | reset when `SqlDialect` changes |
| Built SQL strings (column list, etc.) | per builder | -- | rebuilt with the builder |
| Bulk-operation prepared commands | per adapter | -- | reset when `BulkUpdateKeys` changes |
| Adapter cache | per `DatabaseWrapper` | type | cleared on dispose |

## Lineage (why some things look the way they do)

Zonkey predates every modern .NET ORM and grew up alongside classic ADO -- which explains several choices that would look odd in a green-field design. `DataRowState` reuses the `System.Data` enum rather than defining its own because the concept genuinely is ADO's row-state model applied per object. `Recordset` and `DataTableAdapter` exist because the library has always served applications migrating forward from classic ADO and `DataSet`-era code. The v5 rewrite made everything task-async; the sync variants were removed, and with no sync/async pairs left to disambiguate, the `-Async` suffixes were dropped -- the suffix-less names (`Fill`, `Save`, `GetOne`) *are* the async API.

## See Also

- [Overview & Philosophy](overview.md) -- the values this architecture serves
- [Database Providers & Dialects](database-providers.md) -- per-engine specifics and quoting semantics
- [Data Classes & Attributes](data-classes.md) -- the mapping layer in detail
- [Modeling Relationships](modeling-relationships.md) -- how to work *with* this architecture for related data
