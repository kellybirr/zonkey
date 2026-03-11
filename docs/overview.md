# Overview & Philosophy

## What Zonkey Is

Zonkey is a lightweight ORM for .NET that maps C# objects to database tables using attributes. It provides structured CRUD operations, per-object change tracking, and parameterized query building while keeping every database interaction explicit and predictable.

Zonkey is not a framework. It is a focused library for data access that you compose into your own application architecture. It does not impose patterns on your code beyond the data classes themselves.

## Core Philosophy: Determinism

The central design value of Zonkey is **determinism**. Every database operation is explicit and predictable.

- **You always know what SQL will execute and when.** There is no deferred execution, no query materialization surprises, and no ambient context assembling operations behind the scenes.
- **There is no global change tracking.** No context object silently monitors a graph of loaded entities. When you call `Save`, it affects exactly the object you pass to it -- nothing else.
- **No lazy loading, no hidden cascades, no implicit persistence.** If data is not explicitly loaded, it is not there. If an object is not explicitly saved, it is not persisted.
- **Change tracking is per-object, optional, and observable.** Each `DataClass` instance tracks its own state through `DataRowState` (Added, Modified, Unchanged, Detached) and stores original values in its `OriginalValues` dictionary. You can inspect both at any time. For read-only use cases, you can use plain POCOs with mapping attributes and skip change tracking entirely.

```csharp
// You can always inspect an object's state
Console.WriteLine(product.DataRowState);  // Modified
Console.WriteLine(product.OriginalValues["Price"]);  // 19.99 (original value before change)
```

## Async-First

Since v5.0, all data access operations in Zonkey return `Task`. This is not retrofitted async-over-sync -- the library was redesigned around `async`/`await` from the ground up. All read operations (`GetOne`, `Fill`, `OpenReader`) and all write operations (`Save`, `Delete`, `TrySave`) are natively asynchronous.

```csharp
var product = await adapter.GetOne(p => p.Id == 42);
product.Price = 29.99m;
await adapter.Save(product);
```

See [Async Patterns](async-patterns.md) for detailed guidance.

## Architecture Layers

Zonkey's architecture is composed of a small number of focused components. Each has a single responsibility.

### DataClass + Attributes

The `DataClass` base class and its associated attributes (`DataItemAttribute`, `DataFieldAttribute`) define the mapping between C# objects and database tables. Attributes specify table names, column names, data types, key fields, auto-increment behavior, and nullability.

See [Data Classes & Attributes](data-classes.md) for details.

### DataClassAdapter\<T\>

`DataClassAdapter<T>` is the primary interface for CRUD operations. It provides methods to read (`GetOne`, `Fill`, `FillAll`, `OpenReader`), write (`Save`, `TrySave`, `Insert`, `Update`), and delete (`Delete`, `DeleteItem`) mapped objects. It operates on a single `DbConnection` and a single type `T`.

See [DataClassAdapter (CRUD Operations)](data-class-adapter.md) for details.

### DatabaseWrapper

`DatabaseWrapper` manages a `DbConnection` and caches `DataClassAdapter` instances by type. It provides convenience methods that delegate to the underlying adapters, plus transaction support via `BeginTransaction` and `WithTransaction`.

See [DatabaseWrapper (Connection & Lifecycle)](database-wrapper.md) for details.

### DataManager

`DataManager` executes raw SQL queries and stored procedures. It provides `ExecuteNonQuery`, `ExecuteScalar`, `GetDataReader`, `GetDataSet`, and `FillDataTable` methods with automatic parameter binding.

See [DataManager (Raw SQL)](data-manager.md) for details.

### SqlFilter

`SqlFilter` builds parameterized WHERE clauses using a fluent static API (`SqlFilter.EQ`, `SqlFilter.GT`, `SqlFilter.LIKE`, etc.). Filters are automatically parameterized and dialect-aware.

See [Querying (Filters, LINQ, Pagination)](querying.md) for details.

### Dialect System

The `SqlDialect` base class and its implementations (SQL Server, PostgreSQL, MySQL, Oracle, SQLite, DB2, and a generic fallback) handle database-specific SQL generation. Dialects are auto-detected from the `DbConnection` type.

See [Database Providers & Dialects](database-providers.md) for details.

## What Zonkey Is Not

- **Not a migration framework.** Zonkey does not manage your database schema. It does not create tables, generate migration scripts, or track schema versions. Use your database tools, a migration library, or the [code generation tools](code-generation.md) to keep classes and tables in sync.
- **Not a LINQ-to-SQL query provider.** Zonkey does not implement `IQueryable`. Lambda expressions are supported for WHERE clause generation only. There is no query composition pipeline, no projection, and no server-side grouping through LINQ syntax.
- **Not an application framework.** Zonkey does not dictate your application architecture. It does not provide dependency injection integration, repository base classes, or unit-of-work abstractions. It is a library you call from your own code.
- **Not a replacement for understanding SQL.** Zonkey generates straightforward SELECT, INSERT, UPDATE, and DELETE statements. It expects you to understand your database, your indexes, and your query patterns.

## Design Principles

1. **Explicit over implicit.** Every database operation is a deliberate method call. Nothing happens behind the scenes.
2. **Predictable over convenient.** You may write a few more lines of code, but you always know exactly what will happen when you run them.
3. **Simple over clever.** The generated SQL is readable and unsurprising. The API surface is small and consistent.
4. **One object at a time over bulk magic.** `Save` persists a single object. `SaveCollection` iterates and saves each item individually. There is no unit-of-work that batches unrelated changes.

## Comparison with Other Libraries

### vs Entity Framework / EF Core

Entity Framework is a full-featured ORM with powerful capabilities including LINQ-to-SQL query composition, automatic change tracking across a context, lazy loading, navigation properties, and schema migrations. These features enable rapid development but can also lead to subtle performance and correctness issues: unexpected N+1 queries from lazy loading, large change tracker graphs causing slow `SaveChanges` calls, and implicit cascades persisting objects you did not intend to save.

Zonkey avoids these categories of problems by design. There is no context-level change tracker, no lazy loading, and no implicit cascades. The tradeoff is that you write more explicit code -- loading related data with separate queries, saving each object individually, and managing your own schema.

For a detailed concept-by-concept comparison, see [Migrating from Entity Framework](migrating-from-ef.md).

### vs Dapper

Dapper is an excellent micro-ORM for developers who prefer to write their own SQL. It maps query results to objects with minimal overhead and imposes almost no abstractions.

Zonkey shares Dapper's philosophy of explicit control but adds structured features on top: attribute-based table/column mapping, automatic SELECT/INSERT/UPDATE/DELETE generation, per-object change tracking, and `SqlFilter` for parameterized WHERE clause construction. If you find yourself building these abstractions on top of Dapper, Zonkey provides them out of the box while maintaining a similar level of transparency about what SQL executes.

---

[Back to documentation index](README.md) | [Project README](../README.md)
