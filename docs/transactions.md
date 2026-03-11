# Transactions

Zonkey provides full transaction support at every level of the data access stack. This guide covers two categories of transaction usage: simple single-connection transactions, and distributed multi-connection transactions coordinated through `DbTransactionRegistry`.

## Simple Transactions

For operations that span multiple saves on a single database connection, Zonkey offers three patterns for enrolling adapters in a transaction.

### Explicit Transaction on DataClassAdapter

The most direct approach is to set the `Transaction` property on each adapter instance. This works with raw ADO.NET connections and does not require `DatabaseWrapper`.

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

await using var trx = await connection.BeginTransactionAsync();

var orderAdapter = new DataClassAdapter<Order>(connection) { Transaction = trx };
var lineAdapter = new DataClassAdapter<OrderLine>(connection) { Transaction = trx };

try
{
    var order = new Order(addingNew: true)
    {
        CustomerId = customerId,
        OrderDate = DateTime.UtcNow,
        Status = "pending",
        Total = 49.98m
    };
    await orderAdapter.Save(order);

    var line = new OrderLine(addingNew: true)
    {
        OrderId = order.Id,
        ProductId = productId,
        Quantity = 2,
        UnitPrice = 24.99m
    };
    await lineAdapter.Save(line);

    await trx.CommitAsync();
}
catch
{
    await trx.RollbackAsync();
    throw;
}
```

When `Transaction` is set on an adapter, every command the adapter executes is enrolled in that transaction. Multiple adapters can share the same transaction as long as they use the same connection. The adapter checks for an explicit `Transaction` before falling back to the `DbTransactionRegistry`, so setting it directly always takes priority.

### DatabaseWrapper.BeginTransaction()

When using `DatabaseWrapper`, you can call `BeginTransaction()` to create a transaction on the wrapper's connection, then pass it to `Adapter<T>(trx)` to get adapters with the transaction already assigned.

```csharp
await using var db = await StoreDb.OpenAsync(connectionString);
var trx = db.BeginTransaction();

try
{
    var orderAdapter = db.Adapter<Order>(trx);
    var lineAdapter = db.Adapter<OrderLine>(trx);

    var order = new Order(addingNew: true)
    {
        CustomerId = customerId,
        OrderDate = DateTime.UtcNow,
        Status = "pending",
        Total = 49.98m
    };
    await orderAdapter.Save(order);

    var line = new OrderLine(addingNew: true)
    {
        OrderId = order.Id,
        ProductId = productId,
        Quantity = 2,
        UnitPrice = 24.99m
    };
    await lineAdapter.Save(line);

    trx.Commit();
}
catch
{
    trx.Rollback();
    throw;
}
```

`Adapter<T>(trx)` returns the cached adapter for that type with its `Transaction` property set. This is the recommended pattern for async transaction handling with `DatabaseWrapper`.

### DatabaseWrapper.WithTransaction()

For simple transaction blocks, `WithTransaction` handles the commit-or-rollback lifecycle automatically.

```csharp
await using var db = await StoreDb.OpenAsync(connectionString);

await db.WithTransaction(async trx =>
{
    var orderAdapter = db.Adapter<Order>(trx);
    var lineAdapter = db.Adapter<OrderLine>(trx);

    var order = new Order(addingNew: true) { /* ... */ };
    await orderAdapter.Save(order);

    var line = new OrderLine(addingNew: true) { OrderId = order.Id, /* ... */ };
    await lineAdapter.Save(line);
});
// Auto-commits on success, auto-rolls back on exception
```

`WithTransaction` takes a `Func<DbTransaction, Task>`, so you can use `async`/`await` naturally inside the delegate.

### When to Use Which

| Pattern | Best for |
|---------|----------|
| Explicit adapter `Transaction` | Direct adapter usage without `DatabaseWrapper` |
| `BeginTransaction` + `Adapter<T>(trx)` | Async code with `DatabaseWrapper` (recommended) |
| `WithTransaction` | Async transaction blocks with automatic commit/rollback |

## DbTransactionRegistry -- Distributed Transactions

`DbTransactionRegistry` is a static, thread-safe registry that maps `DbConnection` instances to their active `DbTransaction`. It lives in the `Zonkey` namespace.

Its key feature: once you register a transaction on a connection, all subsequent operations on that connection -- through any `DataClassAdapter`, `DataTableAdapter`, or `DataManager` -- automatically participate in the transaction without any explicit wiring. This is called auto-enrollment.

### How Auto-Enrollment Works

Every time an adapter or `DataManager` executes a command, it checks whether the command should be enrolled in a transaction. The mechanism differs slightly between the two:

**DataClassAdapter and DataTableAdapter** call an internal method named `EnrollInTransaction`. This method:

1. Checks if the adapter has an explicit `Transaction` property set -- if so, assigns that transaction to the command.
2. Otherwise, calls `DbTransactionRegistry.SetCommandTransaction(command)`, which looks up the command's connection in the registry and assigns the registered transaction to the command.

**DataManager** calls `DbTransactionRegistry.SetCommandTransaction(command)` directly on every command it creates. Because `DataManager` does not have an explicit `Transaction` property, it always uses the registry.

This means you can register a transaction once and then use any adapter or `DataManager` on that connection without passing the transaction around. All operations automatically participate.

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Register a transaction -- all operations on this connection now auto-enroll
DbTransactionRegistry.RegisterNewTransaction(connection);

try
{
    var orderAdapter = new DataClassAdapter<Order>(connection);
    var lineAdapter = new DataClassAdapter<OrderLine>(connection);
    var dm = new DataManager(connection);

    // None of these need an explicit transaction -- the registry handles it
    var order = new Order(addingNew: true)
    {
        CustomerId = customerId,
        OrderDate = DateTime.UtcNow,
        Status = "pending",
        Total = 49.98m
    };
    await orderAdapter.Save(order);

    var line = new OrderLine(addingNew: true)
    {
        OrderId = order.Id,
        ProductId = productId,
        Quantity = 2,
        UnitPrice = 24.99m
    };
    await lineAdapter.Save(line);

    await dm.ExecuteNonQuery(
        "UPDATE inventory SET quantity = quantity - $0 WHERE product_id = $1",
        line.Quantity, line.ProductId);

    // Commit
    var trx = DbTransactionRegistry.RetrieveTransaction(connection);
    trx!.Commit();
}
catch
{
    var trx = DbTransactionRegistry.RetrieveTransaction(connection);
    trx?.Rollback();
    throw;
}
finally
{
    DbTransactionRegistry.RemoveTransaction(connection);
}
```

### API Reference

| Method | Description |
|--------|-------------|
| `RegisterNewTransaction(DbConnection)` | Creates a new transaction on the connection and registers it in the registry. |
| `RegisterNewTransaction(DbConnection, IsolationLevel)` | Creates a transaction with a specific isolation level and registers it. |
| `RegisterTransaction(DbConnection, DbTransaction)` | Registers an existing transaction that you created yourself. |
| `RetrieveTransaction(DbConnection)` | Returns the registered transaction for a connection, or `null` if none is registered. |
| `SetCommandTransaction(DbCommand)` | Looks up the transaction for the command's connection and assigns it to the command. Called internally by adapters and `DataManager` -- you rarely call this directly. |
| `RemoveTransaction(DbConnection)` | Removes the registration for a connection. Call this in a `finally` block after commit or rollback. |
| `Clear()` | Removes all registrations. Use with caution -- this affects all connections across all threads. |

### Multi-Database Transactions

This is where `DbTransactionRegistry` provides its most distinctive value. It can coordinate transactions across multiple connections -- even across different database vendors. Each connection gets its own native transaction, and you control when they all commit or all roll back.

```csharp
await using var pgConn = new NpgsqlConnection(pgConnectionString);
await using var sqlConn = new SqlConnection(sqlConnectionString);
await pgConn.OpenAsync();
await sqlConn.OpenAsync();

// Register transactions on both connections
DbTransactionRegistry.RegisterNewTransaction(pgConn);
DbTransactionRegistry.RegisterNewTransaction(sqlConn);

try
{
    // PostgreSQL operations -- auto-enrolled
    var orderAdapter = new DataClassAdapter<Order>(pgConn);
    var order = new Order(addingNew: true)
    {
        CustomerId = customerId,
        OrderDate = DateTime.UtcNow,
        Status = "confirmed",
        Total = 49.98m
    };
    await orderAdapter.Save(order);

    // SQL Server operations -- also auto-enrolled, different database entirely
    var auditAdapter = new DataClassAdapter<AuditEntry>(sqlConn);
    var audit = new AuditEntry(addingNew: true)
    {
        Action = "order_created",
        EntityId = order.Id.ToString(),
        Timestamp = DateTime.UtcNow
    };
    await auditAdapter.Save(audit);

    // Commit both -- order matters, commit the most critical first
    DbTransactionRegistry.RetrieveTransaction(pgConn)!.Commit();
    DbTransactionRegistry.RetrieveTransaction(sqlConn)!.Commit();
}
catch
{
    // Roll back both
    DbTransactionRegistry.RetrieveTransaction(pgConn)?.Rollback();
    DbTransactionRegistry.RetrieveTransaction(sqlConn)?.Rollback();
    throw;
}
finally
{
    DbTransactionRegistry.RemoveTransaction(pgConn);
    DbTransactionRegistry.RemoveTransaction(sqlConn);
}
```

Because each connection uses its own native `DbTransaction`, any ADO.NET provider works. PostgreSQL, SQL Server, MySQL, SQLite -- any combination is valid.

### Comparison with MSDTC

The traditional .NET approach to distributed transactions is `System.Transactions` with the Microsoft Distributed Transaction Coordinator (MSDTC). MSDTC provides true two-phase commit (2PC), guaranteeing atomicity across all participating resources even if a participant crashes mid-commit.

However, MSDTC comes with significant operational overhead:

- **Windows-only**: MSDTC is a Windows service. It does not run on Linux, macOS, or in many containerized environments.
- **DCOM dependency**: MSDTC communicates between machines using DCOM (Distributed COM), a protocol from the 1990s that requires specific port ranges, authentication configuration, and firewall rules. Getting DCOM to work reliably across network boundaries -- especially through firewalls, NAT devices, or cloud security groups -- is a well-known operational challenge.
- **Service configuration**: MSDTC must be installed, running, and identically configured on every machine that participates in the transaction. Misconfiguration on any participant silently breaks the entire distributed transaction.
- **Domain authentication**: In many configurations, MSDTC requires Windows domain authentication between participants, adding Active Directory as an infrastructure dependency.
- **Database support**: Not all database providers support MSDTC enlistment. PostgreSQL, MySQL, and many non-Microsoft databases have limited or no MSDTC support.

Zonkey's `DbTransactionRegistry` takes a different approach. It coordinates transactions at the application level using standard `DbTransaction` objects. This means:

- **Works everywhere**: Any OS, any database, any ADO.NET provider. No infrastructure dependencies.
- **No DCOM**: No network protocol configuration. Transactions are coordinated in-process.
- **No service dependency**: No Windows service to install, configure, or monitor.
- **Cross-vendor**: PostgreSQL, SQL Server, MySQL, SQLite -- any combination works because each connection uses its own native transaction.

**The tradeoff**: `DbTransactionRegistry` provides best-effort coordination, not true two-phase commit. If the first connection commits successfully but the second connection fails to commit (due to a crash, network error, or constraint violation), you have a partial commit. The first connection's changes are persisted while the second connection's changes are rolled back.

For most applications, this tradeoff is acceptable:

- The window between sequential commits is measured in milliseconds.
- True distributed transaction failures (crash between commits) are rare in practice.
- The failure modes are predictable and can be handled with application-level compensation logic (retry, audit log, reconciliation).
- Many applications that used MSDTC were already accepting similar risks due to MSDTC configuration issues in practice.

For applications that truly require guaranteed atomicity across multiple databases, MSDTC (where available) or an application-level saga pattern are the appropriate solutions. `DbTransactionRegistry` is designed for the common case where lightweight coordination is sufficient.

### Thread Safety

`DbTransactionRegistry` is thread-safe. All methods acquire a lock on the internal dictionary before reading or writing. The `SetCommandTransaction` method uses double-locking (registry lock + command lock) to prevent race conditions during concurrent command enrollment. This means multiple threads can safely register, retrieve, and remove transactions on different connections without interfering with each other.

### Best Practices

- **Always use try/catch/finally** when working with the registry. The `finally` block is essential for cleanup.
- **Always call `RemoveTransaction` in a `finally` block.** Failing to remove a registration leaves a stale entry in the static dictionary, which can cause subsequent operations on a recycled connection to use an invalid transaction.
- **Commit the most critical transaction first** in multi-database scenarios. If a failure occurs between commits, the most important data is already persisted.
- **Keep the window between registering and committing as short as possible.** Long-running transactions hold database locks and increase the risk of contention.
- **Prefer simpler patterns when they suffice.** For single-connection transactions, the explicit `Transaction` property or `DatabaseWrapper.BeginTransaction()` is simpler and more direct. Use `DbTransactionRegistry` when you need auto-enrollment across multiple adapters and a `DataManager` on the same connection, or when you need cross-database coordination.

## See Also

- [DatabaseWrapper](database-wrapper.md) -- connection and lifecycle management with built-in transaction helpers
- [DataClassAdapter](data-class-adapter.md) -- full adapter API for querying and saving entities
- [DataManager](data-manager.md) -- raw SQL execution for ad-hoc queries
- [Overview](overview.md) -- Zonkey's design philosophy and architecture
