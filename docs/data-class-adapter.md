# DataClassAdapter -- CRUD Operations

`DataClassAdapter<T>` is the central class for all mapped database operations in Zonkey. It provides typed CRUD operations for any class decorated with `DataItem` and `DataField` attributes. It operates on a single `DbConnection` and auto-detects the SQL dialect based on the connection type.

**Type constraint:** `where T : class` -- T must be a reference type. `ISavable` (via `DataClass`) is not required for read operations; it is only required for save operations such as `Save`, `TrySave`, `Insert`, and `Update`.

---

## Creating an Adapter

```csharp
// From a connection (auto-detects SQL dialect)
var adapter = new DataClassAdapter<Product>(connection);

// With explicit table name override
var adapter = new DataClassAdapter<Product>(connection, "product_archive");

// With key fields override
var adapter = new DataClassAdapter<Product>(connection, "products", ["id"]);

// With a schema version
var adapter = new DataClassAdapter<Product>(connection, schemaVersion: 2);

// With table name, key fields, and schema version
var adapter = new DataClassAdapter<Product>(connection, "products", ["id"], schemaVersion: 2);

// Default constructor (must assign Connection before use)
var adapter = new DataClassAdapter<Product>();
adapter.Connection = connection;
```

### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Connection` | `DbConnection` | The database connection. Auto-detects `SqlDialect` when set. |
| `CommandTimeout` | `int?` | Per-adapter timeout override for all commands. |
| `OrderBy` | `string` | SQL ORDER BY clause appended to SELECT commands. Required for `FillRange`. |
| `NoLock` | `bool` | Enable SQL Server `NOLOCK` hint for read operations. |
| `Transaction` | `DbTransaction` | Enroll all commands in a database transaction. |
| `ObjectFactory` | `Func<T>` | Custom factory for creating T instances during reads. |
| `IgnoreUpdateRowCount` | `bool` | Suppress conflict detection on update (use with caution). |
| `NullStringDefault` | `object` | Default value used when inserting null reference strings. Defaults to `DBNull.Value`. |
| `SqlDialect` | `SqlDialect` | The detected SQL dialect. Can be set explicitly. |

### Static Defaults

| Property | Type | Description |
|----------|------|-------------|
| `DataClassAdapter.DefaultCommandTimeout` | `int?` | Default timeout for all adapters that do not set `CommandTimeout`. |
| `DataClassAdapter.DefaultSchemaVersion` | `int?` | Default schema version for all adapters in the current process. |
| `DataClassAdapter.DefaultQuotedIdentifier` | `bool?` | Default quoted identifier behavior for all adapters. |

### Transactions

When `Transaction` is set on an adapter, every command the adapter executes participates in that transaction. For auto-enrollment without explicitly setting the property, use `DbTransactionRegistry`. See [Transactions](transactions.md) for full details on single-connection and distributed transaction patterns.

---

## Querying -- Fill

`Fill` populates an existing collection. All overloads return `Task<int>` with the count of objects added to the collection.

```csharp
var products = new List<Product>();

// Lambda expression (converted to WHERE clause)
await adapter.Fill(products, p => p.Price < 25.00m);

// SqlFilter array
await adapter.Fill(products, SqlFilter.GT("price", 10.0m), SqlFilter.LT("price", 50.0m));

// String filter with parameters
await adapter.Fill(products, "category = $0", "shirts");

// String filter (no parameters)
await adapter.Fill(products, "price > 10");

// All records
await adapter.FillAll(products);

// From a stored procedure
await adapter.FillWithSP(products, "get_products_by_category", "shirts");

// From a DbCommand
await adapter.Fill(products, myCommand);

// From an existing DbDataReader
await adapter.Fill(products, reader);
```

`$0`, `$1`, `$2`... are positional parameter placeholders. The adapter replaces them with dialect-appropriate parameter syntax (`@p0` for SQL Server, `:p0` for PostgreSQL, etc.) and creates proper `DbParameter` objects.

See [querying.md](querying.md) for detailed filter documentation.

---

## Querying -- GetOne

Returns a single object of type `T`, or `default(T)` (null for reference types) if no matching record is found.

```csharp
var product = await adapter.GetOne(p => p.Id == 42);
var product = await adapter.GetOne(SqlFilter.EQ("id", 42));
var product = await adapter.GetOne("id = $0", 42);
var product = await adapter.GetOne("id = 42");
var product = await adapter.GetOne(myCommand);
```

The adapter automatically optimizes single-row queries via `SqlDialect.OptimizeSelectSingleCommand`, which may add `TOP 1` or `LIMIT 1` depending on the dialect.

---

## Querying -- GetCount and Exists

`GetCount` returns a `Task<long>` with the number of matching rows. `Exists` returns a `Task<bool>` indicating whether any matching rows exist.

```csharp
long count = await adapter.GetCount(p => p.Price > 10.00m);
long count = await adapter.GetCount("category = $0", "shirts");
long count = await adapter.GetCount(SqlFilter.EQ("category", "shirts"));

bool exists = await adapter.Exists(p => p.Name == "Classic Tee");
bool exists = await adapter.Exists("id = $0", 42);
bool exists = await adapter.Exists(SqlFilter.EQ("name", "Classic Tee"));
```

---

## Querying -- OpenReader

For streaming large result sets without loading everything into memory. Returns a `DataClassReader<T>` that reads one object at a time.

```csharp
using var reader = await adapter.OpenReader(p => p.Price < 100.00m);
T product;
while ((product = await reader.ReadAsync()) != null)
{
    // Process each product without holding all in memory
}
```

`DataClassReader<T>` also supports `IEnumerable<T>` and `IAsyncEnumerable<T>`, so you can use it with `foreach` and `await foreach`:

```csharp
using var reader = await adapter.OpenReader(SqlFilter.GT("price", 0));
await foreach (var product in reader)
{
    Console.WriteLine(product.Name);
}
```

OpenReader accepts all the same filter types as Fill:

```csharp
var reader = await adapter.OpenReader(p => p.Category == "shirts");
var reader = await adapter.OpenReader(SqlFilter.EQ("category", "shirts"));
var reader = await adapter.OpenReader("category = $0", "shirts");
var reader = await adapter.OpenReader("category = 'shirts'");
var reader = await adapter.OpenReader(myCommand);
```

You can also wrap an existing `DbDataReader` using `CreateReader`:

```csharp
DataClassReader<Product> reader = adapter.CreateReader(existingDbDataReader);
```

The caller is responsible for disposing the reader.

---

## Querying -- Extension Methods

Convenience extensions in `Zonkey.Extensions` that return `List<T>` or `T[]`:

```csharp
using Zonkey.Extensions;

List<Product> products = await adapter.GetList(p => p.Price < 25.00m);
List<Product> products = await adapter.GetList("price < $0", 25.00m);

Product[] products = await adapter.GetArray(p => p.Price < 25.00m);
Product[] products = await adapter.GetArray("price < $0", 25.00m);
```

These are wrappers around `OpenReader` that collect all results into a list or array.

---

## Saving -- Save and TrySave

`Save` determines whether to insert or update based on `DataRowState` (requires `ISavable`, typically via `DataClass`):

```csharp
// Insert -- DataRowState is Added
var product = new Product(addingNew: true) { Name = "Classic Tee", Price = 24.99m };
await adapter.Save(product);
// product.Id populated via select-back, DataRowState is now Unchanged

// Update -- DataRowState is Modified
product.Price = 19.99m;
await adapter.Save(product);
// Only the price column was updated, DataRowState is now Unchanged
```

`Save` returns `bool` and throws on failure (`UpdateConflictException` on conflict, `SaveFailedException` on other failures). `TrySave` returns a `SaveResult` for non-throwing error handling:

```csharp
SaveResult result = await adapter.TrySave(product);
if (result.Status == SaveResultStatus.Success)
    Console.WriteLine($"Saved as {result.SaveType}"); // Insert or Update
else if (result.Status == SaveResultStatus.Conflict)
    Console.WriteLine("Concurrent modification detected");
```

### Save Overloads

```csharp
await adapter.Save(product, UpdateCriteria.KeyAndVersion);
await adapter.Save(product, SelectBack.AllFields);
await adapter.Save(product, UpdateCriteria.ChangedFields, UpdateAffect.ChangedFields, SelectBack.None);
```

### UpdateCriteria

Controls the WHERE clause of UPDATE statements:

| Value | Behavior |
|-------|----------|
| `Default` | Uses the adapter's default behavior |
| `KeyOnly` | WHERE uses key fields only |
| `KeyAndVersion` | WHERE uses key + RowVersion fields (optimistic concurrency) |
| `ChangedFields` | WHERE includes original values of changed fields |
| `AllFields` | WHERE includes all original field values |

### UpdateAffect

Controls the SET clause of UPDATE statements:

| Value | Behavior |
|-------|----------|
| `ChangedFields` | SET clause only includes fields whose values changed |
| `AllFields` | SET clause includes all writable fields |

### SelectBack

Controls which fields are read back from the database after a save operation:

| Value | Behavior |
|-------|----------|
| `Default` | Uses the adapter's default behavior |
| `None` | No SELECT after save |
| `IdentityOrVersion` | SELECT back identity and version fields only |
| `UnchangedFields` | SELECT back fields that were not in the SET clause |
| `AllFields` | SELECT back all fields |

---

## Saving -- Insert and Update Directly

Skip `DataRowState` detection and perform an explicit insert or update:

```csharp
await adapter.Insert(product, SelectBack.IdentityOrVersion);
await adapter.Update(product, UpdateCriteria.KeyOnly, UpdateAffect.ChangedFields, SelectBack.None);
```

The `TryInsert` and `TryUpdate` variants return `SaveResult` instead of throwing:

```csharp
SaveResult result = await adapter.TryInsert(product, SelectBack.AllFields);
SaveResult result = await adapter.TryUpdate(product, UpdateCriteria.KeyOnly, UpdateAffect.ChangedFields, SelectBack.None);
```

---

## Saving -- Collections

Save an entire collection of objects. Each item is saved according to its `DataRowState`.

```csharp
var products = new List<Product>();
// ... modify products ...

int saved = await adapter.SaveCollection(products);
```

`SaveCollection` throws a `CollectionSaveException<T>` if any item conflicts or fails. For detailed results without throwing, use `TrySaveCollection`:

```csharp
CollectionSaveResult<Product> result = await adapter.TrySaveCollection(products, continueOnError: true);
// result.Inserted  -- IList<T> of inserted items
// result.Updated   -- IList<T> of updated items
// result.Deleted   -- IList<T> of deleted items
// result.Skipped   -- IList<T> of skipped items (Unchanged state)
// result.Conflicted -- IList<T> of conflicted items
// result.Failed    -- IList<T> of failed items
// result.Exceptions -- IList<CollectionSaveExceptionItem<T>>
// result.ErrorCount -- total count of failed + conflicted + exceptions
```

Collection save processes deleted items first (via `ITrackDeletedItems<T>`), then saves added and modified items.

---

## Saving -- Bulk Operations

High-throughput insert and update without change tracking overhead. These methods skip `ISavable` events (`OnBeforeSave`/`OnAfterSave`), skip select-back, and reuse a single prepared command for all objects.

```csharp
// Bulk insert a collection
int inserted = await adapter.BulkInsert(products);

// Bulk insert a single object
await adapter.BulkInsert(product);

// Bulk update a collection
int updated = await adapter.BulkUpdate(products);

// Bulk update a single object
await adapter.BulkUpdate(product);
```

The `BulkUpdateKeys` property controls whether key field values are included in the bulk update SET clause:

```csharp
adapter.BulkUpdateKeys = true;
await adapter.BulkUpdate(products);
```

Use bulk operations for high-volume data loading scenarios where change tracking and select-back are not needed.

---

## Deleting

```csharp
// Delete by lambda expression
int deleted = await adapter.Delete(p => p.Price == 0);

// Delete by SqlFilter
int deleted = await adapter.Delete(SqlFilter.EQ("category", "discontinued"));

// Delete by string filter with parameters
int deleted = await adapter.Delete("created_utc < $0", cutoffDate);

// Delete by string filter (no parameters)
int deleted = await adapter.Delete("price = 0");

// Delete a specific object (by its key fields)
bool deleted = await adapter.DeleteItem(product);
```

The expression and filter-based `Delete` overloads return `Task<int>` with the number of rows deleted. `DeleteItem` returns `Task<bool>` indicating whether exactly one row was deleted. All delete methods throw `DeleteFailedException` on database errors.

---

## UpdateRows (Mass Update)

Update multiple rows in a single statement without loading objects into memory. Returns `Task<int>` with the number of rows affected.

```csharp
// Using an anonymous object for the SET clause
int updated = await adapter.UpdateRows(
    new { Price = 27.49m },
    p => p.Category == "shirts"
);

// Using a dictionary for the SET clause
var values = new Dictionary<string, object>
{
    ["Price"] = 27.49m,
    ["Category"] = "sale"
};
int updated = await adapter.UpdateRows(values, p => p.Category == "shirts");
```

---

## Conflict Detection

When using `UpdateCriteria.KeyAndVersion` or `ChangedFields`, the adapter detects concurrent modifications. After a conflict, use `GetConflicts` to inspect what changed:

```csharp
var result = await adapter.TrySave(product);
if (result.Status == SaveResultStatus.Conflict)
{
    Conflict[] conflicts = await adapter.GetConflicts(product);
    foreach (var c in conflicts)
    {
        Console.WriteLine($"{c.PropertyName}: was {c.OriginalValue}, now {c.CurrentDbValue}, you set {c.AttemptedValue}");
    }
}
```

The `Conflict` class exposes:

| Property | Description |
|----------|-------------|
| `PropertyName` | The name of the conflicting property |
| `OriginalValue` | The value when the object was last loaded |
| `CurrentDbValue` | The current value in the database |
| `AttemptedValue` | The value the application tried to save |
| `SetValueTo` | An optional resolution value (for manual conflict resolution) |

---

## Events

The adapter exposes two events:

### BeforeExecuteCommand

Fires before any command is executed against the database. Set `Cancel = true` on the event args to abort the operation (throws `OperationCanceledException`).

```csharp
adapter.BeforeExecuteCommand += (sender, args) =>
{
    Console.WriteLine($"Executing: {args.Command.CommandText}");
    // args.Cancel = true; // to abort
};
```

### BeforeSave

Fires before a `Save`, `Insert`, or `Update` commits to the database. Only fires when the object implements `ISavable`. Set `Cancel = true` to abort.

```csharp
adapter.BeforeSave += (sender, args) =>
{
    Console.WriteLine($"About to {args.SaveType} a {typeof(Product).Name}");
    // args.DataObject -- the ISavable being saved
    // args.DataMap    -- the DataMap for the object
    // args.Cancel = true; // to abort
};
```

---

## See Also

- [querying.md](querying.md) -- Detailed filter documentation, IN clauses, and pagination
