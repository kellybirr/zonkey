# Zonkey

[![Build and Test](https://github.com/kellybirr/zonkey/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/kellybirr/zonkey/actions/workflows/build-and-test.yml)

[![NuGet: Zonkey.Data](https://img.shields.io/nuget/v/zonkey.data?label=NuGet%3A%20Zonkey.Data)](https://www.nuget.org/packages/Zonkey.Data/)
[![NuGet: Zonkey.Data.MsSql](https://img.shields.io/nuget/v/zonkey.data.mssql?label=NuGet%3A%20Zonkey.Data.MsSql)](https://www.nuget.org/packages/Zonkey.Data.MsSql/)
[![NuGet: Zonkey.Text](https://img.shields.io/nuget/v/zonkey.text?label=NuGet%3A%20Zonkey.Text)](https://www.nuget.org/packages/Zonkey.Text/)
[![NuGet: Zonkey.Mocks](https://img.shields.io/nuget/v/zonkey.mocks?label=NuGet%3A%20Zonkey.Mocks)](https://www.nuget.org/packages/Zonkey.Mocks/)
[![NuGet: zonkey.scaffold](https://img.shields.io/nuget/v/zonkey.scaffold?label=NuGet%3A%20zonkey.scaffold)](https://www.nuget.org/packages/zonkey.scaffold/)

> [!IMPORTANT]
> **Zonkey 7.0 is a major release with breaking changes.** The 7.0 packages target .NET 8, .NET 10, and .NET Framework 4.8, and assemblies are no longer strong-name signed. If your application runs on .NET 7 or earlier (including .NET 5/6 and .NET Core), on a .NET Framework version before 4.8, or requires strong-named assemblies, stay on the latest v6.x release.

**Deterministic data access for .NET.** Zonkey is a lightweight ORM that makes every database operation explicit and predictable. No implicit context, no surprise queries, no hidden persistence — just clean, direct mapping between your objects and your database.

## Why Zonkey?

Most ORMs trade clarity for convenience. Zonkey takes the opposite approach:

- **Every operation is explicit.** You always know what SQL will execute and when. There are no lazy-loading surprises or hidden cascades.
- **Change tracking is per-object, optional, and observable.** Objects track their own state through `DataRowState`. No ambient context, no proxy generation, no change-detection sweeps.
- **Async-first by default.** Since v5.0, all data access operations return `Task`. You opt *in* to blocking, not out of async.
- **No magic.** Attribute-based mapping is straightforward. What you see in the class definition is what maps to the database.
- **Multi-database support.** Built-in dialect system handles SQL Server, PostgreSQL, MySQL, SQLite, Oracle, DB2, and more.

## Quick Start

Install via NuGet:

```shell
dotnet add package Zonkey.Data
```

### Define a data class

```csharp
using System.Data;
using Zonkey.ObjectModel;

[DataItem("products")]
public class Product : DataClass
{
    private int _id;
    private string _name = "";
    private decimal _price;

    public Product(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use Product(bool addingNew) in code.", true)]
    public Product() : this(false) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public int Id { get => _id; set => SetFieldValue(ref _id, value); }

    [DataField("name", DbType.String, false, Length = 100)]
    public string Name { get => _name; set => SetFieldValue(ref _name, value); }

    [DataField("price", DbType.Decimal, false)]
    public decimal Price { get => _price; set => SetFieldValue(ref _price, value); }
}
```

### Query and save

```csharp
using Zonkey;

// Every operation is explicit — you control the connection
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var adapter = new DataClassAdapter<Product>(connection);

// Read a single item
var product = await adapter.GetOne(p => p.Id == 42);

// Query with filters
var affordable = new List<Product>();
await adapter.Fill(affordable, p => p.Price < 25.00m);

// Create and save — state is explicit
var shirt = new Product(addingNew: true) { Name = "Classic Tee", Price = 19.99m };
await adapter.Save(shirt);
// shirt.Id is now populated, shirt.DataRowState is Unchanged

// Modify and save — only changed fields are updated
shirt.Price = 17.99m;
// shirt.DataRowState is now Modified
await adapter.Save(shirt);
```

### Wrap your database

For real applications, subclass `DatabaseWrapper` to manage connections and centralize data access:

```csharp
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

```csharp
await using var db = await StoreDb.OpenAsync(connectionString);

var customer = await db.GetOne<Customer>(c => c.Email == "alice@example.com");
var order = new Order(addingNew: true) { CustomerId = customer.Id, Status = "pending" };
await db.Save(order);
```

## Scaffolding an Existing Database

Don't hand-write data classes for a database you already have. `zonkey-scaffold` is a cross-platform `dotnet tool` that reads a live schema and writes them for you — it replaces the old Windows-only `Zonkey.CodeGen` (WinForms/SMO) and `NpgCodeGen` utilities with a single CLI.

```shell
dotnet tool install -g zonkey.scaffold

zonkey-scaffold --provider mssql \
                --connection "Server=localhost;Database=Store;Trusted_Connection=True" \
                --namespace MyApp.Data \
                --out ./Data
```

Reads **SQL Server, PostgreSQL, MySQL/MariaDB, and SQLite**. Emits one `partial` class per table plus a `DatabaseWrapper`, in **C# or VB.NET** (`--Language VB`), with keys, identity columns, sequences, row versions, lengths, and per-provider `DbType` mapping already filled in.

```csharp
[DataItem("animals")]
public partial class Animal : DataClass
{
    public Animal(bool addingNew) : base(addingNew) { }

    [DataField("animal_id", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
    public int AnimalId { get => field; set => SetFieldValue(ref field, value); }
}
```

Optionally, `--Emit:Relations true` derives in-memory graph members from your foreign keys and generates batched loaders that fetch children for a whole set of parents in **one query**, not one per parent:

```csharp
await db.Orders.Fill(orders, o => o.PlacedOn >= since);
await db.OrderDetails.FillOrderDetailsFor(orders);   // one query for every order
```

Nothing loads implicitly — Zonkey still has no navigation properties or lazy loading, and the generated code adds none.

**The output is a starting point, not a build step.** Read it, rename what you like, and edit it. Add your own members in a separate partial class file so regenerating doesn't overwrite them.

Full options, provider notes, and the agent skill it ships: **[Scaffolding](docs/scaffolding.md)**.

## Packages

| Package | Description |
|---------|-------------|
| [Zonkey.Data](https://www.nuget.org/packages/Zonkey.Data/) | Core ORM library — mapping, querying, and persistence |
| [Zonkey.Data.MsSql](https://www.nuget.org/packages/Zonkey.Data.MsSql/) | SQL Server extensions (XML support, type handling) |
| [Zonkey.Text](https://www.nuget.org/packages/Zonkey.Text/) | CSV and fixed-width text file mapping |
| [Zonkey.Mocks](https://www.nuget.org/packages/Zonkey.Mocks/) | Mock ADO.NET objects for unit testing |
| [zonkey.scaffold](https://www.nuget.org/packages/zonkey.scaffold/) | `dotnet tool` that generates data classes from a live database |

## Source Navigation & Debugging

The NuGet packages are built with **Source Link** and **embedded PDBs**: symbols travel inside the assemblies, stamped with the exact GitHub commit they were built from. Go To Definition (Ctrl+Click / F12) and step-into debugging resolve to the real source on GitHub with no symbol server and no configuration beyond enabling Source Link support in your IDE. The entire repository is public — nothing about how Zonkey works is hidden. AI coding agents: see [`AGENTS.md`](AGENTS.md) for an orientation guide.

## Documentation

Comprehensive documentation is available in the [`docs/`](docs/) folder, written for both developers and AI coding agents:

- [Overview & Philosophy](docs/overview.md) — why Zonkey exists and how it thinks about data access
- [Architecture](docs/architecture.md) — the life of a query and a save, end to end
- [Getting Started](docs/getting-started.md) — installation, first data class, first query
- [Data Classes & Attributes](docs/data-classes.md) — mapping objects to tables with attributes
- [DataClassAdapter](docs/data-class-adapter.md) — the central class for CRUD operations
- [Querying](docs/querying.md) — SqlFilter, LINQ expressions, pagination
- [Modeling Relationships](docs/modeling-relationships.md) — related data without navigation properties
- [DatabaseWrapper](docs/database-wrapper.md) — connection lifecycle, adapter caching, transactions
- [DataManager](docs/data-manager.md) — raw SQL execution and ad-hoc queries
- [Async Patterns](docs/async-patterns.md) — async-first design and usage guidance
- [Transactions](docs/transactions.md) — simple and distributed transaction support
- [DataTableAdapter](docs/data-table-adapter.md) — working with DataTable and DataSet
- [Recordset](docs/recordset.md) — classic ADO-style cursor API
- [Database Providers & Dialects](docs/database-providers.md) — supported databases and dialect system
- [PostgreSQL Guide](docs/postgresql.md) — timestamps, case folding, and provider-specific types
- [Testing with Mocks](docs/testing.md) — unit testing with Zonkey.Mocks
- [Text File Mapping](docs/text-files.md) — CSV and fixed-width files with Zonkey.Text
- [Scaffolding / Code Generation](docs/scaffolding.md) — `zonkey-scaffold`, the CLI that generates data classes from a live database
- [Migrating from Entity Framework](docs/migrating-from-ef.md) — concept mapping for EF developers
- [Upgrading from Zonkey 4.x](docs/upgrading-from-v4.md) — the sync-to-async port, and what stays the same

Each source project also has its own README:

- [`src/Zonkey.Data`](src/Zonkey.Data/) — Core library
- [`src/Zonkey.Data.MsSql`](src/Zonkey.Data.MsSql/) — SQL Server extensions
- [`src/Zonkey.Text`](src/Zonkey.Text/) — Text file mapping
- [`src/Zonkey.Mocks`](src/Zonkey.Mocks/) — Mock objects for testing

## Project Status

Zonkey has been in production use since the early days of .NET and has evolved through every major .NET release. It currently targets .NET 8, .NET 10, and .NET Framework 4.8. As of v7.0, assemblies are no longer strong-name signed; consumers that require strong-named assemblies, or that run on frameworks older than .NET 8 / .NET Framework 4.8, should stay on the latest v6.x release.

This has historically been a single-maintainer project, but contributions are welcome. If you find a bug, have a feature idea, or want to improve the documentation, please [open an issue](https://github.com/kellybirr/zonkey/issues) or submit a pull request.

## Breaking Changes from v6.x

Zonkey 7.0 is a major release. Beyond the target-framework and strong-naming changes called out above, the following behaviors changed:

- **Target frameworks**: now net8.0/net10.0/net48 (netstandard2.x and net6 dropped). `Zonkey.Text` still targets netstandard2.0/net48.
- **Strong naming removed.** Assemblies are no longer strong-name signed; the last signed release remains available on v6.x for consumers that require it.
- **`DatabaseWrapper.WithTransaction` is now async-only**: `Task WithTransaction(Func<DbTransaction, Task> code)` (previously `void WithTransaction(Action<DbTransaction>)`). This is a compile-time break — wrap synchronous work in an async lambda and await the call.
- **`SqlScriptProcessor.ExecuteScript` no longer closes the connection.** Closing/disposing the connection is now the caller's responsibility.
- **WHERE-expression translation fixes**, several of which change query results silently if you don't recompile against v7.0:
  - `Nullable<T>.HasValue` now correctly emits `IS NOT NULL` (v6 emitted the inverted SQL).
  - Wildcard characters (`%`, `_`, `\`, `[`) in `StartsWith`/`EndsWith`/`Contains` arguments now match literally (v6 treated them as SQL wildcards).
  - Untranslatable expressions throw `SqlExpressionException` (derives from `NotSupportedException`) instead of silently mistranslating or falling back to client-side evaluation.
- **SQLite paging (`FillRange`) fixed.** v6 had the `LIMIT`/`OFFSET` operands swapped and returned the wrong page of rows.
- **SQL Server paging (`FillRange`) now emits `OFFSET ... FETCH NEXT ... ROWS ONLY`** and requires **SQL Server 2012 or later**. v6 used a `ROW_NUMBER() OVER(...)` wrapper compatible with SQL Server 2005/2008; that wrapper has been removed. Oracle and DB2 now also support `FillRange` via the same ANSI SQL:2008 offset-fetch form (previously unsupported and threw `NotSupportedException`).
- **`Recordset.MoveLast` now positions on the last row.** v6 landed on EOF and returned `false`.
- **Integral-to-enum conversion now throws for out-of-range values** instead of silently wrapping (v6 behavior).
- **`DataClassReader`'s IL-emitted fast builder is on by default** (`DataClassReader<T>.DefaultUseFastBuilder = true`). Set it to `false` to fall back to the previous per-field reflection path.
- **Obsoleted**: `SqlIn(IEnumerable)`, `SqlInInt`, `SqlInGuid` — use `list.Contains(field)` instead, which now covers everything they did (and more): a single value collapses to `=`, PostgreSQL binds the list as one array parameter, and other dialects parameterize or inline automatically. They still work but are marked `[Obsolete]`. The lambda subquery `SqlIn` overloads (`field.SqlIn((T x) => ...)`) are unaffected and remain the supported way to express `IN (SELECT ...)`.

See [Querying](docs/querying.md#pre-v70-behavior-changes) and [Migrating from Entity Framework](docs/migrating-from-ef.md) for more detail on the expression-translation changes.

## Upgrading from 4.x

Zonkey 4.2 was synchronous and .NET Framework only. Moving to 7.x is a real port — the mapping model survives, but every database call becomes an `await`. See [Upgrading from Zonkey 4.x](docs/upgrading-from-v4.md).

The old `zonkey42` package remains on NuGet and is frozen; staying on it is a legitimate choice if you don't need async or modern targets.

## License

Zonkey is licensed under the [MIT License](LICENSE). It is free for commercial and enterprise use.
