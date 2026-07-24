# Recordset -- Classic ADO-Style Data Access

`Zonkey.Ado.Recordset` is a deliberate throwback: a close analog of the classic ADO `Recordset` object from the VB6 / ASP era, built on modern ADO.NET underneath. If you are migrating code that thinks in `MoveNext`, `EOF`, and `UpdateBatch`, this class lets that code keep its shape while running on Zonkey's dialect system and async I/O.

It is *mostly* API compatible with its ancestor. The differences -- and there are a few important ones -- are called out [below](#differences-from-classic-ado).

---

## Quick Example

```csharp
using Zonkey.Ado;

var rs = new Recordset(connection);          // or new Recordset("connection-name")
await rs.Open("SELECT * FROM products WHERE category = $0", "shirts");

while (!rs.EOF)
{
    Console.WriteLine($"{rs["name"]}: {rs["price"]}");
    rs.MoveNext();
}
```

Any old ADO hand will feel at home immediately -- right down to `SELECT *`, which is not just tolerated here but idiomatic: the Recordset discovers its schema from whatever the query returns.

---

## Opening

Construct with a `DbConnection` or with a connection name registered in [`DbConnectionFactory`](database-wrapper.md#subclassing). `Open` executes the query, opening the connection first if it is closed:

```csharp
await rs.Open("SELECT * FROM products");                          // plain SQL
await rs.Open("SELECT * FROM products WHERE price > $0", 10m);    // $0, $1... parameters
await rs.Open("get_products", CommandType.StoredProcedure, "shirts");
```

`Open` returns the record count. The entire result set is buffered client-side into a `DataTable` -- in ADO terms, you always get `adUseClient` + `adOpenStatic`. There are no server-side cursors, so `RecordCount` is always accurate (never the dreaded `-1`).

`Requery()` re-runs the last query with the same SQL and parameters, refreshing the data and resetting the cursor to the first record.

---

## Navigation

`Position` is the current record index, `BOF` / `EOF` / `RecordCount` behave as you remember (both `BOF` and `EOF` are true on an empty result). The `Move` family positions the cursor:

```csharp
rs.MoveFirst();
rs.MoveNext();
rs.MovePrevious();
rs.Move(5);                       // relative offset, forward or back
rs.FindNext("price > 10");        // forward search from the current position
```

Unlike classic ADO's `void` moves, every navigation method returns `bool` -- `true` while the cursor is on a valid record -- so `while (rs.MoveNext())` loops work without a separate `EOF` check.

`FindNext` is the analog of ADO's `Find`, but takes the full `DataTable.Select` expression syntax (`"name LIKE 'A%' AND price > 10"`), which is considerably more capable than ADO's single-clause criteria.

---

## Reading and Writing Fields

The indexers read and write columns on the current record, by name or ordinal:

```csharp
var name = rs["name"];       // instead of rs.Fields("name").Value
var first = rs[0];
rs["price"] = 19.99m;
```

Accessing the indexers while `BOF` or `EOF` is true throws `InvalidOperationException`. `Fields` returns the underlying `DataColumnCollection` -- schema information only (names, types); values always come through the indexers.

---

## Updating

Updates are batch-only, in the style of `adLockBatchOptimistic`: edit freely, then persist everything in one call. One extra step is required first:

```csharp
await rs.Open("SELECT * FROM products WHERE category = $0", "shirts");
rs.InitUpdate("products", "id");     // table name + primary key column(s)

rs.MoveFirst();
rs["price"] = 19.99m;                // edit the current record

var row = rs.NewRow();               // AddNew, in two steps
row["name"] = "Classic Tee";
row["price"] = 24.99m;
rs.AddRow(row);                      // cursor moves to the new row

rs.MoveFirst();
rs.Delete();                         // marks the current record; move off it before reading

await rs.UpdateBatch();              // one INSERT, one UPDATE, one DELETE
```

`InitUpdate` exists because classic ADO discovered the base table and key server-side; Zonkey runs your arbitrary SQL and cannot know which table to write back to or what uniquely identifies a row, so you say so explicitly. `UpdateBatch` then generates the INSERT / UPDATE / DELETE commands through [`DataTableAdapter`](data-table-adapter.md) and executes them for every pending change.

Details worth knowing:

- **Identity columns work automatically on SQL Server.** A `SELECT *` captures auto-increment and read-only metadata from SqlClient, so inserted rows get their database-assigned keys written back after `UpdateBatch` with no configuration. Npgsql is less forthcoming -- it loads every column read-only and does not flag serial columns -- so on PostgreSQL normalize the flags after `InitUpdate`: `foreach (DataColumn c in rs.Fields) c.ReadOnly = false;` then `rs.Fields["id"].AutoIncrement = true;`. (Harmless on SQL Server too, so provider-agnostic code can always do it.)
- **`Delete` is local until `UpdateBatch`.** After `Delete`, the current record is inaccessible until you move off it (exactly like ADO); the row is removed from the database when the batch runs.
- **Concurrency is last-write-wins.** Generated UPDATE and DELETE statements match on the primary key only -- there is no ADO-style conflict detection against original values.
- **Identifier quoting** defaults to on (`UseQuotedIdentifier = true`), unlike the tri-state default used elsewhere in Zonkey. On PostgreSQL, quoted identifiers are case-sensitive -- use lowercase names in `InitUpdate` (see the [PostgreSQL guide](postgresql.md)).

---

## Differences from Classic ADO

| Classic ADO | Zonkey Recordset |
|---|---|
| `rs.Open src, conn, cursor, lock` | `await rs.Open(sql, params...)` -- async, returns the record count |
| Cursor/lock types (`adOpenDynamic`, `adLockPessimistic`, ...) | Always a client-side static snapshot with batch-optimistic updates; no server-side cursors or locks |
| `AbsolutePosition` (1-based) | `Position` (0-based) |
| `Move*` return nothing | `Move*` return `true` while on a valid record |
| `rs.Fields("name").Value` | `rs["name"]` / `rs[0]` indexers; `Fields` exposes schema only |
| `AddNew` | `NewRow()` + `AddRow(row)` |
| `Update` (per-row) / `UpdateBatch` | `UpdateBatch` only -- and `InitUpdate(table, keys...)` must be called first |
| `CancelUpdate` / `CancelBatch` | Not available -- `Requery()` to discard pending changes |
| `Find` | `FindNext(expression)` with full `DataTable.Select` syntax |
| Conflict detection on changed values | Primary-key-only WHERE clauses (last write wins) |
| `Filter`, `Sort`, `Bookmark`, `GetRows`, `NextRecordset`, paging, events | Not available |
| `rs.Close` leaves the connection open | `Close()` / `Dispose()` **also close the connection** -- don't wrap a shared connection's Recordset in `using` if you need the connection afterward |

That last row deserves emphasis: `Dispose` closes the `Connection` it was given. When the Recordset owns its connection (the connection-name constructor), `using` is exactly right; when you pass a shared connection, call `Close` only when you are done with the connection too.

---

## When to Use It

Reach for `Recordset` when porting classic ADO code, or when its cursor idiom genuinely fits the problem. For new code working with runtime-discovered schemas, [`DataTableAdapter`](data-table-adapter.md) offers the same dynamic SQL generation without the cursor ceremony, and [`DataClassAdapter`](data-class-adapter.md) is the type-safe choice whenever the schema is known at compile time.

[Back to overview](overview.md) | [Back to README](../README.md)
