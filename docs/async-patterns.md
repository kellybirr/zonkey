# Async Patterns

## Async-First Design

Since v5.0, Zonkey is async-first. All data access methods return `Task` or `Task<T>`:

```csharp
Task<T> GetOne(...)
Task<int> Fill(...)
Task<bool> Save(...)
Task<int> Delete(...)
Task<long> GetCount(...)
Task<bool> Exists(...)
Task<object> ExecuteScalar(...)
Task<int> ExecuteNonQuery(...)
```

This is not async wrappers around synchronous code. The underlying ADO.NET calls use native async methods (`ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`).

## Method Naming

Zonkey's async methods do not carry the .NET-conventional `-Async` suffix — the async API is `Fill`, `Save`, `GetOne`, not `FillAsync`. Earlier versions paired synchronous methods with `TaskAsync`-suffixed async variants (e.g. `FillTaskAsync`); when the synchronous variants were dropped, the async methods took the plain names, and there is no sync/async pair left to disambiguate. Do not rename or search for `FillAsync` — the suffix-less names are the async API.

## Standard Usage

```csharp
// Normal async/await — the expected pattern
var product = await adapter.GetOne(p => p.Id == 42);
await adapter.Save(product);

var products = new List<Product>();
await adapter.Fill(products, p => p.Price < 25.00m);
```

## ConfigureAwait

Internally, Zonkey uses `.ConfigureAwait(false)` on most async operations. This means:

- Library methods generally do not capture the synchronization context
- This is correct behavior for a library
- Your calling code should follow standard async best practices for your framework (ASP.NET Core does not need `ConfigureAwait`, but WinForms/WPF callers should be aware of potential cross-thread issues)

## Sequential vs Parallel

Database connections are not thread-safe. Do not call multiple async adapter methods concurrently on the same connection:

```csharp
// CORRECT — sequential
var products = new List<Product>();
await adapter.Fill(products, p => p.Category == "shirts");

var customers = new List<Customer>();
await customerAdapter.Fill(customers, c => c.IsActive);

// WRONG — parallel on same connection
// await Task.WhenAll(
//     adapter.Fill(products, ...),
//     customerAdapter.Fill(customers, ...)
// );
```

If you need parallel queries, use separate connections:

```csharp
var productTask = Task.Run(async () =>
{
    await using var db = await StoreDb.OpenAsync(connectionString);
    return await db.Adapter<Product>().GetList(p => p.Category == "shirts");
});

var customerTask = Task.Run(async () =>
{
    await using var db = await StoreDb.OpenAsync(connectionString);
    return await db.Adapter<Customer>().GetList(c => c.IsActive);
});

var products = await productTask;
var customers = await customerTask;
```

For transactional operations across multiple adapters, see [Transactions](transactions.md) — Zonkey's `DbTransactionRegistry` provides auto-enrollment so all operations on a registered connection participate in the same transaction without explicit wiring.

## Loading Related Data

A common async pattern is to load primary data, extract IDs, then load related data:

```csharp
await using var db = await StoreDb.OpenAsync(connectionString);

// Load orders
var orders = new List<Order>();
await db.Adapter<Order>().Fill(orders, o => o.Status == "pending");

// Load order lines for those orders
var orderIds = orders.Select(o => o.Id).ToArray();
var lines = new List<OrderLine>();

foreach (int[] batch in orderIds.Chunk(2000))
{
    await db.Adapter<OrderLine>().Fill(lines, l => batch.Contains(l.OrderId));
}

// Join in memory
var orderDetails = orders.Select(o => new
{
    Order = o,
    Lines = lines.Where(l => l.OrderId == o.Id).ToList()
});
```

`list.Contains(field)` translates to a SQL `IN (...)` clause. `Chunk` (from `System.Linq`) breaks large ID lists into batches to stay within SQL parameter limits. Whether you need it depends on the dialect and the key type: PostgreSQL binds any list as a single array parameter and has no ceiling, and on the other dialects integer and `Guid` lists inline as literals past 64 values, so neither needs batching. It is strings, dates and decimals on SQL Server, SQLite and MySQL that stay parameterized and can hit the cap -- so the loop above is illustrative rather than required for `int` ids. See [Translation policy](querying.md#translation-policy).

## Cancellation

`DataClassAdapter` supports `CancellationToken` through a protected property on the base class `AdapterBase2`. All internal command executions (`ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`) pass this token. If you need cancellation support, you can subclass the adapter:

```csharp
public class CancellableAdapter<T> : DataClassAdapter<T> where T : class, new()
{
    public CancellableAdapter(DbConnection connection, CancellationToken ct)
        : base(connection)
    {
        CancellationToken = ct;
    }
}
```

Usage:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var adapter = new CancellableAdapter<Product>(connection, cts.Token);

var products = new List<Product>();
await adapter.Fill(products, p => p.Category == "shirts");
```

## Database Connection Lifecycle

Connections should be short-lived in web applications:

```csharp
// ASP.NET Core controller pattern
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id)
{
    await using var db = await StoreDb.OpenAsync(_connectionString);
    var product = await db.GetOne<Product>(p => p.Id == id);
    return product is not null ? Ok(product) : NotFound();
}
```

For background services or batch jobs, a longer-lived connection is appropriate:

```csharp
await using var db = await StoreDb.OpenAsync(connectionString);

foreach (var batch in items.Chunk(100))
{
    foreach (var item in batch)
    {
        await db.Save(item);
    }
}
```

## See Also

- [DatabaseWrapper](database-wrapper.md) — connection management and lifecycle patterns
