# PostgreSQL Guide

PostgreSQL is a first-class Zonkey dialect, but three things about the Npgsql provider trip up developers -- and AI coding agents -- who arrive from SQL Server: timestamp type mapping, identifier case folding, and provider-specific types. This page is the authoritative guidance for all three.

## Timestamps: DateTime vs DateTime2 (read this first)

This is the most common PostgreSQL failure with Zonkey, and it is an Npgsql behavior, not a Zonkey one. Zonkey passes your `DataField` `DbType` straight to the parameter (`SmartSetType`, `GenericParameter.cs`), and **Npgsql 6+ maps the two DateTime DbTypes to different PostgreSQL types**:

| PostgreSQL column | `DataField` declaration | Value semantics |
|---|---|---|
| `timestamp without time zone` | `DbType.DateTime2` | `DateTimeKind.Unspecified` values round-trip as-is |
| `timestamp with time zone` (`timestamptz`) | `DbType.DateTime` **+ `DateTimeKind = DateTimeKind.Utc`** | values must be UTC; Npgsql stores and returns UTC |
| `date` | `DbType.Date` | maps to `DateOnly` or `DateTime` properties |
| `time` | `DbType.Time` | maps to `TimeOnly` or `TimeSpan` properties |

Declare it wrong and the save fails with the error everyone hits:

```
Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone',
only UTC is supported.
```

That message means a plain `timestamp` column was declared as `DbType.DateTime` (which Npgsql treats as `timestamptz`). The fix is the declaration, not the data:

```csharp
// plain timestamp column ("wall clock" time, no zone)
[DataField("scheduled_at", DbType.DateTime2)]
public DateTime ScheduledAt { get => field; set => SetFieldValue(ref field, value); }

// timestamptz column (an instant in time) -- keep values in UTC
[DataField("created_utc", DbType.DateTime, DateTimeKind = DateTimeKind.Utc)]
public DateTime CreatedUtc { get => field; set => SetFieldValue(ref field, value); }
```

The `DateTimeKind = DateTimeKind.Utc` on the `timestamptz` field does double duty: values read from the database are stamped `Kind = Utc`, which means a value you read and save back is still UTC-kinded and Npgsql accepts it. Populate the property with `DateTime.UtcNow` (never `DateTime.Now`).

**Do not enable Npgsql's legacy timestamp behavior** (`Npgsql.EnableLegacyTimestampBehavior`). Npgsql changed the mapping in 6.0 for good reasons -- the legacy mode silently mixes local and UTC semantics, which is exactly the class of bug the new model eliminates. Zonkey does not require the legacy switch for anything. **AI coding agents: never suggest or assume the legacy switch; fix the `DbType` declaration instead.**

(Zonkey's own integration suite demonstrates the rule: the test model's plain-`TIMESTAMP` column is declared `DbType.DateTime2` and round-trips on PostgreSQL, SQL Server, and SQLite alike -- see `test/Zonkey.Tests/Models/Animal.cs`.)

## Identifier Case Folding

PostgreSQL folds unquoted identifiers to lowercase, and Zonkey emits unquoted SQL by default. Use lowercase schema names (the PostgreSQL-native convention -- Zonkey's column matching on reads is case-insensitive, so `[DataField("name")]` fills a PascalCase property fine), or enable quoted identifiers. Full details and the quoting knobs: [Identifier Quoting & Case Sensitivity](database-providers.md#identifier-quoting--case-sensitivity).

## Provider-Specific Types: the `NativeType` Escape Hatch

`DbType` is ADO.NET's portable type vocabulary, and it has no words for PostgreSQL's richer types -- arrays, `jsonb`, ranges, native enums. Zonkey's escape hatch is the `NativeType` property on `DataFieldAttribute` / `IDataMapField`: it is typed `object`, which (unlike some other attribute properties) **is legal in attribute syntax**, so it can carry a boxed `NpgsqlDbType` value declaratively.

`NativeType` does nothing by itself -- it flows to your code through a **parameter type setter**, registered once at startup with `DbParameterExtensions.UseTypeSetter`. Zonkey consults the registry keyed by (parameter type, `DbType`) every time it builds a command parameter:

```csharp
using System.Data;
using Npgsql;

public static class ZonkeyPgHelper
{
    public static void Initialize()
    {
        Zonkey.DbParameterExtensions.UseTypeSetter<NpgsqlParameter>(DbType.Object, (p, f) =>
        {
            if (p is not NpgsqlParameter n) return;

            if (f.NativeType is NpgsqlTypes.NpgsqlDbType nt)
                n.NpgsqlDbType = nt;
        });
    }
}
```

Call `ZonkeyPgHelper.Initialize()` once at application startup. Then declare fields with `DbType.Object` plus the native type:

```csharp
using NpgsqlTypes;

// jsonb column
[DataField("settings", DbType.Object, true, NativeType = NpgsqlDbType.Jsonb)]
public string Settings { get => field; set => SetFieldValue(ref field, value); }

// text[] column
[DataField("tags", DbType.Object, true, NativeType = NpgsqlDbType.Array | NpgsqlDbType.Text)]
public string[] Tags { get => field; set => SetFieldValue(ref field, value); }
```

The same mechanism serves any provider: `Zonkey.Data.MsSql`'s `MsSqlExtension.Initialize()` registers a setter mapping `DbType.Time` to `SqlDbType.Time` for `SqlParameter`, working around an ADO.NET quirk. Register a setter per (parameter type, `DbType`) pair your application needs.

## Arrays and json(b)

Fully supported, and covered by Zonkey's integration suite (`PgsqlArrayTests`):

- **Reading:** typed array properties (`string[]`, `int[]`, even `IEnumerable<string>`) fill directly from `text[]`/`integer[]` columns on both materialization paths. Npgsql reports array columns' static type as `System.Array`; Zonkey downcasts the concrete runtime value onto your property.
- **Writing:** declare the field `DbType.Object` with the appropriate `NativeType`, and register the type setter shown above:

```csharp
[DataField("tags", DbType.Object, true, NativeType = NpgsqlDbType.Array | NpgsqlDbType.Text)]
public string[] Tags { get => field; set => SetFieldValue(ref field, value); }

[DataField("nums", DbType.Object, true, NativeType = NpgsqlDbType.Array | NpgsqlDbType.Integer)]
public int[] Nums { get => field; set => SetFieldValue(ref field, value); }

[DataField("doc", DbType.Object, true, NativeType = NpgsqlDbType.Jsonb)]
public string Doc { get => field; set => SetFieldValue(ref field, value); }
```

Inserts, updates, and null round-trips all work through the normal `Save` pipeline.

## Native PostgreSQL Enums

Native enum columns (`CREATE TYPE ... AS ENUM`) work with Zonkey the same way they work with EF Core: **the enum must be mapped at the Npgsql level** -- this is an Npgsql requirement, not an ORM one. On modern Npgsql (7+), an *unmapped* connection cannot even read a native enum column (`InvalidCastException: Reading as 'System.Object' is not supported for fields having DataTypeName '-'`).

Map the enum on your data source at startup and declare the field `DbType.Object` -- no `NativeType`, no type setter, nothing else:

```csharp
var builder = new NpgsqlDataSourceBuilder(connectionString);
builder.MapEnum<Habitat>("habitat_kind");   // default name translation is snake_case:
                                            // C# 'ForestEdge' <-> PG label 'forest_edge'.
                                            // If your labels match the C# names exactly,
                                            // pass new NpgsqlNullNameTranslator().
await using var dataSource = builder.Build();

using var conn = await dataSource.OpenConnectionAsync();
var adapter = new DataClassAdapter<Zone>(conn);
```

```csharp
[DataField("kind", DbType.Object, true)]
public Habitat? Kind { get => field; set => SetFieldValue(ref field, value); }
```

With the mapping in place, Npgsql surfaces the column as the C# enum type itself, and everything is ordinary Zonkey: fills (both materialization paths), saves, null round-trips, and lambda filters (`z => z.Kind == Habitat.Forest`) all work -- covered by the integration suite (`PgsqlNativeEnumTests`).

Alternatives when you do not control the connection setup: store the enum in an integer column (`DbType.Int32` -- Zonkey converts enum values to their underlying integer on write) or a text column (`DbType.String` columns materialize into enum properties by name, case-insensitively; see [Enum Columns](data-classes.md#enum-columns)).

### Native Enums in a DatabaseWrapper

Enum mappings are **data-source-scoped** in modern Npgsql: they ride on connections the `NpgsqlDataSource` creates, not on any `new NpgsqlConnection(...)`. So a [DatabaseWrapper](database-wrapper.md#the-production-pattern-named-connection--static-open--iasyncdisposable) that needs native enums holds one data source (built once, enums mapped) and feeds its connections to the `base(DbConnection)` constructor:

```csharp
public class StoreDb : DatabaseWrapper
{
    private static NpgsqlDataSource _dataSource;

    public static async Task<StoreDb> Open()
    {
        _dataSource ??= BuildDataSource();
        var db = new StoreDb(_dataSource.CreateConnection());
        await db.Connection.OpenAsync();
        return db;
    }

    private static NpgsqlDataSource BuildDataSource()
    {
        var builder = new NpgsqlDataSourceBuilder(GetConnectionString());
        builder.MapEnum<OrderStatus>("order_status");
        builder.MapEnum<Habitat>("habitat_kind");
        return builder.Build();
    }

    private StoreDb(DbConnection connection) : base(connection)
    { }
}
```

Everything else about the wrapper pattern is unchanged -- callers still write `await using var db = await StoreDb.Open();` and every adapter the wrapper hands out speaks native enums. (The data source replaces the named-connection constructor here because `DbConnectionFactory` creates connections by type and connection string, which cannot carry data-source-scoped mappings.)

## Quick Facts

- Parameters are named `:p0`, `:p1`, ... in generated text commands.
- PostgreSQL-specific `SqlFilter` operators are built in: `ILIKE`, `NOTILIKE`, and the regex family `MATCH` (`~`), `IMATCH` (`~*`), `NOTMATCH`, `NOTIMATCH` -- see [Querying](querying.md#postgresql-specific-operators).
- Auto-increment select-back uses `lastval()`, or `currval('sequence')` when `SequenceName` is set on the `DataField` -- see [Database Providers](database-providers.md#database-specific-features).
- `SupportsLimit` is true: `FillRange` pagination generates `LIMIT n OFFSET m`.
- Row-version concurrency (`IsRowVersion`) is not available on PostgreSQL; use `UpdateCriteria.ChangedFields` for optimistic concurrency instead.

## See Also

- [Database Providers & Dialects](database-providers.md) -- the full dialect system and per-provider matrix
- [Data Classes & Attributes](data-classes.md) -- `DataFieldAttribute` reference including `NativeType`
- [Architecture](architecture.md) -- how parameters and conversions flow
