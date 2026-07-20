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

## Native Enums, Arrays, and json(b): Current Support

- **Native enum columns (reading):** Npgsql surfaces PostgreSQL enum values as strings, and Zonkey materializes string-sourced enums into enum properties by name or numeric string, case-insensitively (see [Enum Columns](data-classes.md#enum-columns)). Reading native enums into C# enum properties works out of the box.
- **Native enum columns (writing):** Zonkey's PostgreSQL dialect converts enum parameter values to their underlying integer (`PostgreSqlDialect.FixParameter`), which suits integer columns but not native enum columns. For native enum columns, map the enum at the Npgsql level (`NpgsqlDataSourceBuilder.MapEnum<T>()`) or store text.
- **Arrays (reading):** Npgsql returns array columns as .NET arrays; when the element types line up (e.g., `text[]` into `string[]`), Zonkey's materializer assigns them directly.
- **Arrays and json(b) (writing):** use the `NativeType` setter pattern above.

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
