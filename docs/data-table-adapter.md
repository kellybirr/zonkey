# DataTableAdapter -- DataTable & DataSet Support

`DataTableAdapter` provides Fill and SaveChanges operations for ADO.NET `DataTable` and `DataSet` objects instead of mapped data classes. It is for scenarios where strongly-typed mapping is not appropriate.

`DataTableAdapter` extends `AdapterBase2` (the same base class as `DataClassAdapter`), so it shares connection management, transaction support, and dialect detection.

---

## Overview

`DataTableAdapter` shines when the schema is not known at compile time but is discoverable at runtime. Typical scenarios:

- Customer-defined data shapes -- user-configurable columns, per-tenant custom fields, dynamic forms
- Cases where the dreaded `SELECT *` is genuinely necessary: the adapter generates INSERT / UPDATE / DELETE from whatever columns come back, so unknown-at-compile-time schemas get full CRUD without any mapped classes
- Working with existing typed DataSets from legacy codebases
- Reporting or analytics queries that span multiple tables
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

**The DataTable's columns define the SELECT list**, so `FillAll` and the `Fill` overloads require the columns to exist before the call -- a fresh, column-less `DataTable` produces a SELECT with an empty column list. SQL Server rejects the statement (`Incorrect syntax near the keyword 'FROM'`); PostgreSQL technically accepts an empty projection and returns no columns at all. Either way, no usable data. Define the columns you want, and only those are queried -- the column set acts as a projection:

```csharp
var dt = new DataTable("products");
dt.Columns.Add("id", typeof(int));
dt.Columns.Add("name", typeof(string));
dt.Columns.Add("price", typeof(decimal));

var adapter = new DataTableAdapter(connection);

// All rows
adapter.FillAll(dt);

// With string filter (may reference columns outside the projection)
adapter.Fill(dt, "price > 10");

// With string filter and parameters
adapter.Fill(dt, "category = $0", "shirts");

// With SqlFilter array
adapter.Fill(dt, SqlFilter.GT("price", 10.0m), SqlFilter.LT("price", 50.0m));

// From stored procedure -- runs the proc as-is, so this one DOES work on a
// column-less DataTable: columns are created from the result set
adapter.FillWithSP(dt, "get_products_by_category", "shirts");
```

The `$0`, `$1`, etc. placeholders in string filters are replaced with dialect-appropriate parameter names (`@p0` for SQL Server, `:p0` for PostgreSQL, `?` for positional-only databases).

When you don't know the columns up front, fill the table with [`DataManager.FillDataTable`](#runtime-discovered-schemas-the-select--workflow) instead and let the query define them.

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

Generated UPDATE and DELETE statements match rows on the primary key only -- there is no optimistic-concurrency check against original values, so concurrent edits are last-write-wins.

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

## Runtime-Discovered Schemas: the SELECT * Workflow

This is the combination the whole class exists for: when the schema is only knowable at runtime, let a query define the `DataTable`, then hand the result back to `DataTableAdapter` for full CRUD. `DataManager.FillDataTable` loads whatever the query returns -- and the provider's schema metadata comes with it, so auto-increment and read-only columns are flagged automatically:

```csharp
var dm = new DataManager(connection);
var dt = new DataTable();

// Schema discovered from the result set -- the dreaded SELECT * is the point here
await dm.FillDataTable(dt, "SELECT * FROM products", CommandType.Text);

// Tell the adapter what the query couldn't: target table and primary key
dt.TableName = "products";
dt.PrimaryKey = new[] { dt.Columns["id"] };

// Full CRUD against columns you never declared
dt.Rows[0]["price"] = 19.99m;

var row = dt.NewRow();
row["name"] = "Classic Tee";
row["price"] = 24.99m;
dt.Rows.Add(row);

var adapter = new DataTableAdapter(connection);
int affected = adapter.SaveChanges(dt);   // UPDATE + INSERT; row["id"] now holds the new identity
```

Two things must be set by hand -- `TableName` and `PrimaryKey` -- because a result set cannot reveal which table to write back to or what uniquely identifies a row. Everything else (column names, types, identity columns) is discovered.

**Provider note:** how much schema metadata arrives with the fill varies by provider. SqlClient flags identity columns (`AutoIncrement` + `ReadOnly`) and leaves data columns writable, so the code above works as-is on SQL Server. Npgsql loads *every* column read-only and does not flag serial/identity columns, so on PostgreSQL normalize the flags after the fill:

```csharp
dt.Columns["id"].AutoIncrement = true;            // Npgsql doesn't flag serial columns
foreach (DataColumn c in dt.Columns)
    if (c.ColumnName != "id") c.ReadOnly = false; // Npgsql loads all columns read-only
```

(Setting both on every provider is harmless -- on SqlClient they are already correct -- so provider-agnostic code can always include this normalization.)

`DataManager.FillDataTable` is also useful purely for reading when the query does not map to a single table (joins, aggregates, reporting queries):

```csharp
var dt = new DataTable();
await dm.FillDataTable(dt,
    "SELECT p.name, COUNT(ol.id) as order_count, SUM(ol.quantity) as total_sold " +
    "FROM products p JOIN order_lines ol ON p.id = ol.product_id " +
    "GROUP BY p.name ORDER BY total_sold DESC",
    CommandType.Text);

foreach (DataRow row in dt.Rows)
    Console.WriteLine($"{row["name"]}: {row["total_sold"]} sold");
```

It is asynchronous and accepts a SQL string, a `CommandType`, and optional parameters. Results like these have no single base table, so they are read-only in practice -- there is nothing meaningful to `SaveChanges` to.

---

## Recordset

`Zonkey.Ado.Recordset` layers a classic ADO-style cursor API (`Open`, `MoveNext`, `EOF`, `UpdateBatch`, ...) over this same machinery -- `UpdateBatch` persists changes through a `DataTableAdapter`. It is the natural entry point when migrating VB6 / classic ASP-era code. See the dedicated [Recordset guide](recordset.md) for the full API and the differences from its ADO ancestor.

---

## When to Use DataTableAdapter

- **Prefer [DataClassAdapter](data-class-adapter.md)** when you have defined data classes. It is type-safe and supports change tracking, lambda expressions for filtering, and strongly-typed results.
- **Use DataTableAdapter** for dynamic schemas, legacy typed DataSets, or when `DataTable` is the required output format.
- **Use [DataManager](data-manager.md)** for ad-hoc queries that do not need `DataTable`'s change tracking, or for raw SQL execution (scalar, non-query, data reader).

[Back to overview](overview.md) | [Back to README](../README.md)
