# Zonkey

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=kellybirr_zonkey&metric=alert_status)](https://sonarcloud.io/dashboard?id=kellybirr_zonkey)

[![NuGet: Zonkey.Data](https://img.shields.io/nuget/v/zonkey.data?label=NuGet%3A%20Zonkey.Data)](https://www.nuget.org/packages/Zonkey.Data/)
[![NuGet: Zonkey.Text](https://img.shields.io/nuget/v/zonkey.text?label=NuGet%3A%20Zonkey.Text)](https://www.nuget.org/packages/Zonkey.Text/)
[![NuGet: Zonkey.Droid](https://img.shields.io/nuget/v/zonkey.droid?label=NuGet%3A%20Zonkey.Droid)](https://www.nuget.org/packages/Zonkey.Droid/)
[![NuGet: Zonkey.Mocks](https://img.shields.io/nuget/v/zonkey.mocks?label=NuGet%3A%20Zonkey.Mocks)](https://www.nuget.org/packages/Zonkey.Mocks/)

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

    public Product() : base(false) { }
    public Product(bool addingNew) : base(addingNew) { }

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
using var db = await StoreDb.OpenAsync(connectionString);

var customer = await db.GetOne<Customer>(c => c.Email == "alice@example.com");
var order = new Order(addingNew: true) { CustomerId = customer.Id, Status = "pending" };
await db.Save(order);
```

## Packages

| Package | Description |
|---------|-------------|
| [Zonkey.Data](https://www.nuget.org/packages/Zonkey.Data/) | Core ORM library — mapping, querying, and persistence |
| [Zonkey.Data.MsSql](https://www.nuget.org/packages/Zonkey.Data.MsSql/) | SQL Server extensions (XML support, type handling) |
| [Zonkey.Text](https://www.nuget.org/packages/Zonkey.Text/) | CSV and fixed-width text file mapping |
| [Zonkey.Mocks](https://www.nuget.org/packages/Zonkey.Mocks/) | Mock ADO.NET objects for unit testing |

## Documentation

Comprehensive documentation is available in the [`docs/`](docs/) folder:

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
- [Database Providers & Dialects](docs/database-providers.md) — supported databases and dialect system
- [Testing with Mocks](docs/testing.md) — unit testing with Zonkey.Mocks
- [Text File Mapping](docs/text-files.md) — CSV and fixed-width files with Zonkey.Text
- [Code Generation Tools](docs/code-generation.md) — generating data classes from database schemas
- [Migrating from Entity Framework](docs/migrating-from-ef.md) — concept mapping for EF developers

Each source project also has its own README:

- [`src/Zonkey.Data`](src/Zonkey.Data/) — Core library
- [`src/Zonkey.Data.MsSql`](src/Zonkey.Data.MsSql/) — SQL Server extensions
- [`src/Zonkey.Text`](src/Zonkey.Text/) — Text file mapping
- [`src/Zonkey.Mocks`](src/Zonkey.Mocks/) — Mock objects for testing

## Project Status

Zonkey has been in production use since the early days of .NET and has evolved through every major .NET release. It currently targets .NET 6, .NET 8, .NET 10, and .NET Framework 4.8. As of v6.6, assemblies are no longer strong-name signed; consumers that require strong-named assemblies should stay on 6.5.x.

This has historically been a single-maintainer project, but contributions are welcome. If you find a bug, have a feature idea, or want to improve the documentation, please [open an issue](https://github.com/kellybirr/zonkey/issues) or submit a pull request.

## Upgrading from 4.x

If upgrading from Zonkey 4.x or earlier, use Visual Studio's regex Find/Replace to update `SetFieldValue()` calls:

**Find:** `SetFieldValue\(("\w+"), ref (\w+), value\);`
**Replace:** `SetFieldValue(ref $2, value);`

## License

Zonkey is licensed under the [MIT License](LICENSE). It is free for commercial and enterprise use.
