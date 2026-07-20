# DatabaseWrapper — Connection & Lifecycle Management

DatabaseWrapper is an abstract base class you subclass to manage database connections and centralize data access for your application. It is the recommended entry point for production applications.

## Overview

DatabaseWrapper wraps a `DbConnection`, caches `DataClassAdapter` instances per type, and provides convenience methods for common operations. It implements `IDisposable`.

Why use it:

- **Centralizes connection management** in one place
- **Caches adapters** (one per type) for reuse within a connection lifetime
- **Exposes convenience methods** (`GetOne`, `Save`, `OpenReader`) without needing to create adapters manually
- **Provides transaction support** via `BeginTransaction` and `WithTransaction`
- **Disposable** — the connection is cleaned up when the wrapper is disposed

## Subclassing

The factory pattern with a private constructor and async `Open` method is the recommended approach. It ensures the connection is open before any operations.

```csharp
using System.Data.Common;
using Npgsql;
using Zonkey;
using Zonkey.ObjectModel;

public class StoreDb : DatabaseWrapper
{
    private StoreDb(DbConnection connection) : base(connection)
    {
        DataManager = new DataManager(Connection);
    }

    public DataManager DataManager { get; }

    public static async Task<StoreDb> OpenAsync(string connectionString)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        return new StoreDb(conn);
    }
}
```

DatabaseWrapper also provides a named-connection constructor that uses `DbConnectionFactory` to create a connection from a registered name:

```csharp
// Using DbConnectionFactory (connection name from configuration)
protected DatabaseWrapper(string connectionName)
```

This constructor calls `DbConnectionFactory.CreateConnection(connectionName)` internally. You must register connection types with `DbConnectionFactory.Register` before using this constructor.

Note that `DbConnectionFactory.CreateConnection` creates the connection but does not open it, and adapters throw when given an unopened connection. You are responsible for opening `Connection` before performing any operations (or use `DbConnectionFactory.OpenConnection(name)` with the `DbConnection` constructor instead).

### The Production Pattern: Named Connection + Static Open + IAsyncDisposable

A field-proven shape that combines the pieces above: the named-connection constructor (so connection configuration lives in one registration at startup), a static `Open()` factory that opens the connection before anyone can touch it, and `IAsyncDisposable` implemented on the subclass so callers get `await using` back:

```csharp
public class StoreDb : DatabaseWrapper, IAsyncDisposable
{
    public const string Name = "Store";   // registered at startup with DbConnectionFactory.Register

    public static async Task<StoreDb> Open()
    {
        var db = new StoreDb();
        await db.Connection.OpenAsync();
        return db;
    }

    private StoreDb() : base(Name)
    { }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return Connection.DisposeAsync();
    }
}
```

```csharp
await using var db = await StoreDb.Open();
```

The private constructor makes `Open()` the only way in, so an unopened wrapper can never escape. Typed convenience methods (thin one-liners over `Adapter<T>()`) round out the class:

```csharp
public Task<List<Tdc>> GetList<Tdc>(Expression<Func<Tdc, bool>> filter) where Tdc : class, new()
    => Adapter<Tdc>().GetList(filter);

public Task<SaveResult> TrySave<Tdc>(Tdc obj, SelectBack selectBack) where Tdc : class, new()
    => Adapter<Tdc>().TrySave(obj, selectBack);
```

For a variant of this pattern whose connections come from an `NpgsqlDataSource` (required for native PostgreSQL enums), see the [PostgreSQL Guide](postgresql.md#native-enums-in-a-databasewrapper).

## Usage

```csharp
using var db = await StoreDb.OpenAsync(connectionString);

// Convenience methods — no adapter needed
var product = await db.GetOne<Product>(p => p.Id == 42);
await db.Save(product);

// Get an adapter for more control
var adapter = db.Adapter<Product>();
adapter.OrderBy = "name ASC";
var products = new List<Product>();
await adapter.Fill(products, p => p.Price < 50.00m);
```

## Convenience Methods

DatabaseWrapper exposes these methods directly, delegating to the cached adapter for each type:

```csharp
Task<T> GetOne<T>(Expression<Func<T, bool>> expression) where T : class, new()
Task<DataClassReader<T>> OpenReader<T>(Expression<Func<T, bool>> expression) where T : class, new()
Task<bool> Save<T>(T obj) where T : class, ISavable, new()
Task<bool> Save<T>(T obj, UpdateCriteria criteria) where T : class, ISavable, new()
Task<bool> Save<T>(T obj, SelectBack selectBack) where T : class, ISavable, new()
Task<bool> Save<T>(T obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack) where T : class, ISavable, new()
```

`Save` requires `ISavable` (which `DataClass` implements), but `GetOne` and `OpenReader` work with any attributed class that has a parameterless constructor.

## Adapter Access

```csharp
// Get a cached adapter for a type
DataClassAdapter<Product> adapter = db.Adapter<Product>();

// Get an adapter enrolled in a transaction
DataClassAdapter<Product> adapter = db.Adapter<Product>(transaction);
```

Adapters are cached by type in a `ConcurrentDictionary` and reused across calls within the lifetime of the wrapper. When you pass a transaction to `Adapter<T>(trx)`, the transaction is set on the adapter before it is returned.

### Caveats

Because adapters are shared per wrapper instance, calls through the wrapper affect the same adapter object:

- `Adapter<T>()` (the no-transaction overload) sets `Transaction = null` on the cached adapter. The convenience methods (`GetOne`, `Save`, `OpenReader`) use this overload, so calling one mid-transaction un-enrolls that type's adapter from the transaction.
- Per-call configuration such as `OrderBy` set on a cached adapter persists for later calls that reuse the same adapter.
- A single wrapper instance is not safe to use concurrently with different transactions.

## Transactions

### Manual Transaction

```csharp
var trx = db.BeginTransaction();
try
{
    var orderAdapter = db.Adapter<Order>(trx);
    await orderAdapter.Save(order);

    var lineAdapter = db.Adapter<OrderLine>(trx);
    await lineAdapter.Save(orderLine);

    trx.Commit();
}
catch
{
    trx.Rollback();
    throw;
}
```

### WithTransaction Helper

```csharp
await db.WithTransaction(async trx =>
{
    var orderAdapter = db.Adapter<Order>(trx);
    await orderAdapter.Save(order);

    var lineAdapter = db.Adapter<OrderLine>(trx);
    await lineAdapter.Save(orderLine);
});
// Auto-commits on success, rolls back on exception
```

`WithTransaction` takes a `Func<DbTransaction, Task>`, so you can use `async`/`await` naturally inside the delegate. It auto-commits when the delegate completes successfully, and auto-rolls back on any exception.

For auto-enrollment across multiple adapters without passing the transaction explicitly, and for coordinating transactions across multiple databases, see [Transactions](transactions.md).

## Adding Domain Methods

Real-world wrappers often add domain-specific methods that encapsulate common data access patterns:

```csharp
using Zonkey.Extensions;

public class StoreDb : DatabaseWrapper
{
    // ... constructor, OpenAsync, DataManager ...

    public async Task<List<Product>> GetProductsByCategory(string category)
    {
        var adapter = Adapter<Product>();
        return await adapter.GetList(p => p.Category == category);
    }

    public async Task<Order> CreateOrder(int customerId, List<OrderLine> lines)
    {
        var order = new Order(addingNew: true)
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Status = "pending",
            Total = lines.Sum(l => l.Quantity * l.UnitPrice)
        };

        await Save(order);

        foreach (var line in lines)
        {
            line.OrderId = order.Id;
            await Save(line);
        }

        return order;
    }
}
```

## Disposal

DatabaseWrapper implements `IDisposable`. When disposed:

- The adapter cache is cleared
- The underlying `DbConnection` is disposed

Always use `using`:

```csharp
using var db = await StoreDb.OpenAsync(connectionString);
// ... use db ...
// connection is automatically closed and disposed
```

## See Also

- [DataClassAdapter](data-class-adapter.md) — full adapter API for querying and saving entities
- [DataManager](data-manager.md) — raw SQL execution for ad-hoc queries
- [Async Patterns](async-patterns.md) — async best practices with Zonkey
