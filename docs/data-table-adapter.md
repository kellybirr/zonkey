# DataTableAdapter -- DataTable & DataSet Support

`DataTableAdapter` provides Fill and SaveChanges operations for ADO.NET `DataTable` and `DataSet` objects instead of mapped data classes. It is for scenarios where strongly-typed mapping is not appropriate.

`DataTableAdapter` extends `AdapterBase2` (the same base class as `DataClassAdapter`), so it shares connection management, transaction support, and dialect detection.

---

## Overview

Use cases for `DataTableAdapter`:

- Dynamic queries where the schema is not known at compile time
- Working with existing typed DataSets from legacy codebases
- Reporting or analytics queries that span multiple tables
- Quick prototyping without defining data classes
- Interop with systems that consume `DataTable` or `DataSet`

---

## Creating an Adapter

`DataTableAdapter` takes a `DbConnection`. The table name comes from the `DataTable.TableName` property, which must match the database table name.

```csharp
using Zonkey.Ado;

var dt = new DataTable("products");
var adapter = new DataTableAdapter(connection);
```

How the table name appears in the generated SQL depends on `DataTableCommandBuilder.UseQuotedIdentifier`, a tri-state `bool?` that defaults to `null`: SQL Server and SQLite bracket-quote by default, while PostgreSQL and MySQL leave the name unquoted. On PostgreSQL an unquoted identifier case-folds to lowercase, so `new DataTable("Products")` queries `products` -- use lowercase table names there, or set `UseQuotedIdentifier` via a custom `CreateCommandBuilder` delegate. The generated SQL is never schema-qualified, so the table must be reachable through the connection's default schema or search path.

You can also use the default constructor and assign the connection later:

```csharp
var adapter = new DataTableAdapter();
adapter.Connection = connection;
```

Properties inherited from `AdapterBase2`:

- `Connection` -- the `DbConnection`
- `SqlDialect` -- auto-detected from the connection type
- `Transaction` -- for transactional operations
- `ParameterPrefix` -- prefix character for indexed parameters (default `$`). Note: setting this has no effect on `DataTableAdapter` -- its Fill methods call a static `DataManager.AddParamsToCommand` overload that always uses `$`.

Properties on `DataTableAdapter`:

- `SequenceName` -- for databases using sequences (PostgreSQL, Oracle)
- `CreateCommandBuilder` -- delegate that creates the `DataTableCommandBuilder`; can be overridden for custom command generation
- `BeforeSaveChanges` event -- hook for pre-save logic; receives a `TableSaveEventArgs` and can cancel the operation

---

## Filling Data

All Fill methods are synchronous. They populate the `DataTable` using the table's `TableName` property to build SQL.

```csharp
var dt = new DataTable("products");
var adapter = new DataTableAdapter(connection);

// All rows
adapter.FillAll(dt);

// With string filter
adapter.Fill(dt, "price > 10");

// With string filter and parameters
adapter.Fill(dt, "category = $0", "shirts");

// With SqlFilter array
adapter.Fill(dt, SqlFilter.GT("price", 10.0m), SqlFilter.LT("price", 50.0m));

// From stored procedure
adapter.FillWithSP(dt, "get_products_by_category", "shirts");
```

The `$0`, `$1`, etc. placeholders in string filters are replaced with dialect-appropriate parameter names (`@p0` for SQL Server, `:p0` for PostgreSQL, `?` for positional-only databases).

---

## Saving Changes

`SaveChanges` persists inserts, updates, and deletes tracked by the `DataTable`'s row state. It returns the number of rows affected.

```csharp
// Add a row
var row = dt.NewRow();
row["name"] = "Classic Tee";
row["price"] = 24.99m;
dt.Rows.Add(row);

// Modify a row
dt.Rows[0]["price"] = 19.99m;

// Delete a row
dt.Rows[1].Delete();

// Persist all changes
int affected = adapter.SaveChanges(dt);

// Or from a DataSet by table name
int affected = adapter.SaveChanges(dataSet, "products");

// Or by table index
int affected = adapter.SaveChanges(dataSet, 0);
```

The `BeforeSaveChanges` event fires before the update is submitted. Setting `Cancel = true` on the `TableSaveEventArgs` throws an `OperationCanceledException`.

```csharp
adapter.BeforeSaveChanges += (sender, args) =>
{
    // Inspect args.DbAdapter or args.Table
    // Set args.Cancel = true to abort
};
```

If the `DataTable` has an auto-increment primary key column, `SaveChanges` uses the dialect's identity retrieval mechanism (`SCOPE_IDENTITY()` for SQL Server, `LAST_INSERT_ID()` for MySQL, sequence values for PostgreSQL/Oracle). Set `SequenceName` when working with databases that use sequences:

```csharp
adapter.SequenceName = "products_id_seq";
adapter.SaveChanges(dt);
```

---

## Typed DataSets

`DataTableAdapter` works with Visual Studio-generated typed DataSets from earlier .NET versions. If you have a legacy typed DataSet, you can use it directly:

```csharp
var ds = new StoreDataSet();
var adapter = new DataTableAdapter(connection);
adapter.FillAll(ds.Products);

// Modify via strongly-typed rows
ds.Products[0].Price = 19.99m;

adapter.SaveChanges(ds.Products);
```

This provides a bridge for codebases with existing typed DataSet infrastructure.

---

## Dynamic Querying with DataManager

For truly dynamic queries where you write raw SQL, combine `DataManager` with `DataTable`. This is useful when the query does not map to a single table:

```csharp
var dm = new DataManager(connection);
var dt = new DataTable();

await dm.FillDataTable(dt,
    "SELECT p.name, COUNT(ol.id) as order_count, SUM(ol.quantity) as total_sold " +
    "FROM products p JOIN order_lines ol ON p.id = ol.product_id " +
    "GROUP BY p.name ORDER BY total_sold DESC",
    CommandType.Text);

foreach (DataRow row in dt.Rows)
    Console.WriteLine($"{row["name"]}: {row["total_sold"]} sold");
```

`DataManager.FillDataTable` is asynchronous and accepts a SQL string, a `CommandType`, and optional parameters. Unlike `DataTableAdapter`, it does not provide `SaveChanges` -- it is read-only.

---

## Recordset

`Zonkey.Ado.Recordset` is a classic-ADO-style companion API for code migrated from ADO Recordset patterns. It wraps a `DataTable` behind cursor-style navigation: `Open` executes a query (SQL text with optional parameters, or a stored procedure via `CommandType`) and returns the record count, `Requery` re-runs the last query, and `MoveFirst` / `MoveLast` / `MoveNext` / `MovePrevious` / `Move(offset)` / `FindNext(filterExpression)` position the cursor. `BOF`, `EOF`, `RecordCount`, `Position`, and `Fields` mirror their ADO counterparts, and string or ordinal indexers read and write column values on the current row.

Construct it with a `DbConnection` or a registered connection name; `Open` opens the connection automatically if it is closed. For updates, call `InitUpdate(tableName, primaryKey...)` to set the table name and primary key, modify rows via the indexers, `NewRow` / `AddRow`, and `Delete`, then call `UpdateBatch` to persist all pending changes through a `DataTableAdapter` and `DataTableCommandBuilder`.

```csharp
using Zonkey.Ado;

using var rs = new Recordset(connection);
await rs.Open("SELECT id, name, price FROM products WHERE category = $0", "shirts");

while (!rs.EOF)
{
    Console.WriteLine($"{rs["name"]}: {rs["price"]}");
    rs.MoveNext();
}

rs.InitUpdate("products", "id");
rs.MoveFirst();
rs["price"] = 19.99m;
await rs.UpdateBatch();
```

Note that `Recordset.UseQuotedIdentifier` defaults to `true` -- unlike the adapters' tri-state default -- so `UpdateBatch` generates quoted identifiers on every dialect unless you set it to `false`.

---

## When to Use DataTableAdapter

- **Prefer [DataClassAdapter](data-class-adapter.md)** when you have defined data classes. It is type-safe and supports change tracking, lambda expressions for filtering, and strongly-typed results.
- **Use DataTableAdapter** for dynamic schemas, legacy typed DataSets, or when `DataTable` is the required output format.
- **Use [DataManager](data-manager.md)** for ad-hoc queries that do not need `DataTable`'s change tracking, or for raw SQL execution (scalar, non-query, data reader).

[Back to overview](overview.md) | [Back to README](../README.md)
