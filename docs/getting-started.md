# Getting Started

This guide walks you through installing Zonkey, defining your first data class, and performing basic database operations.

## Installation

Install the core package via the .NET CLI:

```shell
dotnet add package Zonkey.Data
```

Optional packages are available for specific scenarios:

- **Zonkey.Data.MsSql** -- SQL Server-specific extensions (e.g., `SqlXmlAdapter`)
- **Zonkey.Text** -- reading and writing delimited text files (CSV, TSV)
- **Zonkey.Mocks** -- test doubles for unit testing without a live database

## Your First Data Class

A Zonkey data class maps a C# class to a database table. Each class uses a pair of attributes -- `DataItemAttribute` on the class and `DataFieldAttribute` on each property -- plus the `DataClass` base class for change tracking.

```csharp
using System.Data;
using Zonkey.ObjectModel;

[DataItem("products")]
public class Product : DataClass
{
    private int _id;
    private string _name = "";
    private decimal _price;
    private string? _description;
    private DateTime _createdUtc;

    public Product() : base(false) { }
    public Product(bool addingNew) : base(addingNew) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public int Id { get => _id; set => SetFieldValue(ref _id, value); }

    [DataField("name", DbType.String, false, Length = 100)]
    public string Name { get => _name; set => SetFieldValue(ref _name, value); }

    [DataField("price", DbType.Decimal, false)]
    public decimal Price { get => _price; set => SetFieldValue(ref _price, value); }

    [DataField("description", DbType.String, true, Length = 500)]
    public string? Description { get => _description; set => SetFieldValue(ref _description, value); }

    [DataField("created_utc", DbType.DateTime, false, DateTimeKind = DateTimeKind.Utc)]
    public DateTime CreatedUtc { get => _createdUtc; set => SetFieldValue(ref _createdUtc, value); }
}
```

The pattern is consistent across every property: a private backing field paired with a public property whose setter calls `SetFieldValue`. This is the mechanism that enables change tracking. When you set a property, `SetFieldValue` records the original value (on first change) and transitions the object's `DataRowState` from `Unchanged` to `Modified`.

The two constructors serve distinct purposes:

- **Parameterless constructor** (`base(false)`) -- used by the adapter when it creates instances during query operations like `Fill` and `GetOne`. The object starts in the `Detached` state, then transitions to `Unchanged` after the adapter populates it and calls `CommitValues`. This constructor must be `public`: the materializer instantiates through it (via generated IL), and throws if it is missing -- unless you supply a custom `adapter.ObjectFactory`.
- **Bool constructor** (`base(addingNew: true)`) -- used when you create a new record in application code. Passing `true` sets `DataRowState` to `Added`, which tells the adapter to perform an INSERT when you call `Save`.

## Your First Query

With a data class defined, querying the database requires only a connection and a `DataClassAdapter`:

```csharp
using Zonkey;

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var adapter = new DataClassAdapter<Product>(connection);

// Get one product by ID
var product = await adapter.GetOne(p => p.Id == 42);

// Get all products under $25
var products = new List<Product>();
await adapter.Fill(products, p => p.Price < 25.00m);

// Check if a product exists
bool exists = await adapter.Exists(p => p.Name == "Classic Tee");

// Count products
long count = await adapter.GetCount(p => p.Price > 10.00m);
```

The adapter auto-detects the SQL dialect from the connection type. A `NpgsqlConnection` produces PostgreSQL syntax, a `SqlConnection` produces SQL Server syntax, and so on. No manual configuration is needed.

One PostgreSQL-specific note: Zonkey emits unquoted identifiers by default, and PostgreSQL folds unquoted identifiers to lowercase. Use lowercase table and column names in your PostgreSQL schema (as this example does), or enable quoted identifiers on the adapter. See [Database Providers & Dialects](database-providers.md) for the details.

## Your First Save

Creating, inserting, and updating records follows a natural pattern driven by `DataRowState`:

```csharp
// Create a new product
var product = new Product(addingNew: true)
{
    Name = "Classic Tee",
    Price = 24.99m,
    Description = "100% cotton classic t-shirt",
    CreatedUtc = DateTime.UtcNow
};
// product.DataRowState == DataRowState.Added

await adapter.Save(product);
// product.Id is now populated from the database
// product.DataRowState == DataRowState.Unchanged

// Update it
product.Price = 19.99m;
// product.DataRowState == DataRowState.Modified

await adapter.Save(product);
// Only the price column is updated
// product.DataRowState == DataRowState.Unchanged
```

When `DataRowState` is `Added`, `Save` performs an INSERT and selects back auto-generated values (such as the identity column). When `DataRowState` is `Modified`, `Save` performs an UPDATE that targets only the changed columns. After a successful save, `CommitValues` is called automatically, clearing the tracked changes and setting the state back to `Unchanged`. If the object is already `Unchanged`, `Save` skips it entirely.

## Using DatabaseWrapper

In production applications, you typically manage the connection and adapters through a subclass of `DatabaseWrapper`. This gives you a single disposable object that owns the connection and caches adapter instances:

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

Usage becomes straightforward:

```csharp
using var db = await StoreDb.OpenAsync(connectionString);

var product = await db.GetOne<Product>(p => p.Id == 42);
product.Price = 17.99m;
await db.Save(product);
```

`DatabaseWrapper` caches `DataClassAdapter` instances internally, so repeated calls to `GetOne`, `Save`, and other convenience methods reuse the same adapter for each type. It also exposes `BeginTransaction` and `WithTransaction` for transactional workflows.

This is the preferred pattern for structuring database access in production applications. See [DatabaseWrapper](database-wrapper.md) for the full API reference.

## Next Steps

- [Data Classes & Attributes](data-classes.md) -- detailed reference for `DataItemAttribute`, `DataFieldAttribute`, and `DataClass`
- [DataClassAdapter](data-class-adapter.md) -- full adapter API including `Fill`, `GetOne`, `Save`, `TrySave`, and bulk operations
- [Querying](querying.md) -- lambda expressions, string filters, `SqlFilter`, sorting, and paging
- [DatabaseWrapper](database-wrapper.md) -- connection management, transactions, and the wrapper pattern
