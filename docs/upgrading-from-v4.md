# Upgrading from Zonkey 4.x

Zonkey 4.2 was the last release of the original line: synchronous, .NET Framework only, shipped as the `zonkey42` NuGet package. Everything from v5 onward is async-first and .NET Standard / .NET Core first, while still supporting .NET Framework 4.8.

This is a real port, not a version bump. The mapping model survives almost unchanged — your `[DataItem]` and `[DataField]` attributes, your key and identity flags, the `DataClass(bool addingNew)` constructor — but every call that touches the database becomes an `await`.

## You do not have to move

**`zonkey42` remains on NuGet and is frozen.** It has not changed in years and is not going anywhere. If an application is stable on 4.2 and does not need async, new frameworks, or the newer providers, leaving it there is a legitimate decision.

Move when you want one of: `async`/`await` throughout, .NET 8/10, PostgreSQL/MySQL/SQLite support, LINQ expression filters translated to SQL, or the code generator.

## At a glance

| | 4.2 | 7.0.1 |
|---|---|---|
| Package | `zonkey42` | `Zonkey.Data` |
| Target | .NET Framework 4.0 | net8.0, net10.0, net48 |
| Data access | synchronous | async, `Task`-returning |
| Async model | event-based `FillAsync` + `FillAsyncComplete` + `AbortFill` | standard TAP (`await`) |
| Single row | `GetSingleItem(...)` | `GetOne(...)` |
| Change tracking | `SetFieldValue("Name", ref _name, value)` | `SetFieldValue(ref _name, value)` |
| `DatabaseWrapper` | concrete, `IDisposable` | abstract, `IDisposable` + `IAsyncDisposable` |
| Transactions | manual | `WithTransaction(Func<DbTransaction, Task>)` |

## 1. Everything becomes async

The single largest change. Method **names are unchanged** — Zonkey's async API is deliberately suffix-less, so there is no `FillAsync` to rename to. What changes is the return type and the `await`:

```csharp
// 4.2
int count = adapter.Fill(products, p => p.Price < 25.00m);
Product one = adapter.GetSingleItem(p => p.Id == 42);
bool saved  = adapter.Save(product);
int deleted = adapter.Delete(p => p.Discontinued);

// 7.0.1
int count = await adapter.Fill(products, p => p.Price < 25.00m);
Product one = await adapter.GetOne(p => p.Id == 42);
bool saved  = await adapter.Save(product);
int deleted = await adapter.Delete(p => p.Discontinued);
```

Do **not** paper over this with `.Result` or `.Wait()`. Zonkey uses `ConfigureAwait(false)` internally, but blocking on a `Task` from a UI or classic ASP.NET context is how you get a deadlock. Let `async` propagate up to your entry point. See [Async Patterns](async-patterns.md).

### The old async model is gone

4.2's asynchrony was event-based and predates TAP:

```csharp
// 4.2 — no longer exists
adapter.AsyncThreadMode = ThreadMode.Background;
adapter.FillAsyncComplete += (s, e) => { /* ... */ };
adapter.FillAsync(products, filters);
adapter.AbortFill();
```

Replace the whole pattern with `await adapter.Fill(...)`. Cancellation, which `AbortFill` approximated, is now a `CancellationToken` on the adapter — see [Async Patterns](async-patterns.md#cancellation).

`FillProgress` still exists for progress reporting during a long fill.

## 2. `SetFieldValue` no longer takes the field name

The name now comes from `[CallerMemberName]`, so the first argument goes away. Visual Studio regex Find/Replace handles it:

**Find:** `SetFieldValue\(("\w+"), ref (\w+), value\);`
**Replace:** `SetFieldValue(ref $2, value);`

```csharp
// 4.2
public string Name { get => _name; set => SetFieldValue("Name", ref _name, value); }

// 7.0.1
public string Name { get => _name; set => SetFieldValue(ref _name, value); }
```

On .NET 10 you can go further and drop the backing field entirely with the `field` keyword — see [Data Classes](data-classes.md#using-the-field-keyword-net-10).

## 3. Your data classes mostly survive

The attribute model is the same. What to check:

- **The parameterless constructor.** Both versions need one for the materializer. In current Zonkey it must be marked `[Obsolete("...", true)]` so application code cannot call it — `new Product()` yields a `Detached` object, and saving one throws. See [Data Classes](data-classes.md#constructor).
- **Enum columns.** Integral-to-enum conversion now throws for out-of-range values instead of silently wrapping.
- **`DataClassReader`** uses an IL-emitted fast builder by default. Set `DataClassReader<T>.DefaultUseFastBuilder = false` to fall back to reflection if a conversion misbehaves.

## 4. `DatabaseWrapper` is now abstract

In 4.2 you could instantiate it directly. Now it is a base class you subclass, which is also where connection lifetime and adapter caching live:

```csharp
public class StoreDb : DatabaseWrapper
{
    private StoreDb(DbConnection connection) : base(connection) { }

    public static async Task<StoreDb> OpenAsync(string connectionString)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        return new StoreDb(conn);
    }
}
```

It implements `IAsyncDisposable` outside .NET Framework, so prefer `await using`. See [DatabaseWrapper](database-wrapper.md).

## 5. Filters gained LINQ expressions

4.2's `SqlFilter` and string filters both still work, unchanged. What is new is that lambda expressions are translated to SQL:

```csharp
await adapter.Fill(products, p => p.Category == "shirts" && p.Price < 25.00m);
```

These are not `IQueryable` — they generate a WHERE clause and execute immediately. See [Querying](querying.md).

## 6. Providers

4.2 was written around SQL Server and OLE DB. Current Zonkey has a dialect system covering SQL Server, PostgreSQL, MySQL, SQLite, Oracle, DB2 and Access, and you supply the ADO.NET provider yourself — Zonkey.Data takes no driver dependency. See [Database Providers & Dialects](database-providers.md).

If you are moving to PostgreSQL, read [the PostgreSQL guide](postgresql.md) first: the `timestamp` versus `timestamptz` distinction has a wrong answer that fails at save time.

## 7. Generating classes for an existing database

If the port means re-deriving data classes from your schema, `zonkey-scaffold` does it:

```shell
dotnet tool install -g zonkey.scaffold
zonkey-scaffold --provider mssql --connection "..." --namespace MyApp.Data --out ./Data
```

It emits C# or VB.NET in the current constructor and `SetFieldValue` shape. See [Code Generation](code-generation.md).

## Also read

Upgrading from 4.x means passing through the v6 → v7 changes as well. The behavioral ones bite silently:

- [Breaking Changes from v6.x](../README.md#breaking-changes-from-v6x) — expression-translation fixes, paging changes, `WithTransaction` becoming async.
- [Querying](querying.md#pre-v70-behavior-changes) — the WHERE-clause specifics.
