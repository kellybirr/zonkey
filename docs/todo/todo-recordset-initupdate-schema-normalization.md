# TODO: Normalize provider schema flags in InitUpdate / DataTableAdapter.PrepareForSave

**Status:** Designed 2026-07-24, deliberately not implemented yet. Design reviewed with maintainer; this doc captures the agreed direction so the work can be picked up cold.

## The problem

The runtime-discovered-schema workflow (`Recordset.Open` / `DataManager.FillDataTable` filling a
`DataTable` via `DataTable.Load`, then writing back through `DataTableAdapter.SaveChanges` or
`Recordset.UpdateBatch`) depends on the reader schema the provider supplies, and providers differ
sharply (verified empirically 2026-07-24 against live containers; regression-tested in
`Integration/DataTableAdapterTests.cs` and `Integration/RecordsetUpdateTests.cs`):

| Provider | Behavior after `DataTable.Load` from `SELECT *` |
|---|---|
| Microsoft.Data.SqlClient | Identity column flagged `AutoIncrement` + `ReadOnly`; data columns writable. Whole flow works with no configuration. |
| Npgsql | **Every** column `ReadOnly = true`; serial/identity columns **not** flagged `AutoIncrement`. Updates throw `ReadOnlyException`; inserts throw `NoNullAllowedException` (NewRow leaves the unflagged serial key null while `AllowDBNull=false` came through). |

Today PostgreSQL consumers must hand-normalize (`ReadOnly = false` on data columns,
`AutoIncrement = true` on the serial key) — boilerplate currently documented in
docs/data-table-adapter.md ("Provider note") and docs/recordset.md, and duplicated in the two
integration test helpers (`PrepareForSave` in DataTableAdapterTests, `NormalizeSchemaFlags` in
RecordsetUpdateTests).

Note the flag damage happens at load time, but the right moment to normalize is when the caller
*declares write intent* — `InitUpdate` for Recordset, and a new explicit call for the raw flow.
`DataTableAdapter.Fill` / `FillAll` are unaffected (DbDataAdapter.Fill does not apply
ReadOnly/AllowDBNull; only `DataTable.Load` does).

## Agreed design

One static helper on `DataTableAdapter` (serves the raw flow directly and replaces the documented
boilerplate); `Recordset.InitUpdate` delegates to it and gains an identity-aware overload.

```csharp
// DataTableAdapter.cs
public static void PrepareForSave(DataTable table, string tableName, params string[] primaryKey)
    => PrepareForSave(table, tableName, false, primaryKey);

public static void PrepareForSave(DataTable table, string tableName, bool autoIncrementKey, params string[] primaryKey)
{
    if (table == null) throw new ArgumentNullException(nameof(table));
    if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

    table.TableName = tableName;
    table.PrimaryKey = primaryKey.Select(s => table.Columns[s]).ToArray();

    if (autoIncrementKey)
    {
        if (table.PrimaryKey.Length != 1)
            throw new ArgumentException("autoIncrementKey requires a single-column primary key", nameof(autoIncrementKey));

        table.PrimaryKey[0].AutoIncrement = true;   // no-op on SqlClient; required on Npgsql
    }

    // Npgsql's blanket is detectable: when ALL columns are read-only, clear the flag on
    // everything except auto-increment keys. SqlClient schemas are mixed (only identity /
    // computed columns read-only) and are left untouched.
    bool allReadOnly = table.Columns.Cast<DataColumn>().All(c => c.ReadOnly);
    if (allReadOnly)
    {
        foreach (DataColumn column in table.Columns)
            if (!column.AutoIncrement)
                column.ReadOnly = false;
    }
}
```

```csharp
// Recordset.cs
public void InitUpdate(string tableName, params string[] primaryKey)
{
    if (_dt == null) throw new InvalidOperationException("Must `Open` Before updating.");
    DataTableAdapter.PrepareForSave(_dt, tableName, primaryKey);
}

public void InitUpdate(string tableName, bool autoIncrementKey, params string[] primaryKey)
{
    if (_dt == null) throw new InvalidOperationException("Must `Open` Before updating.");
    DataTableAdapter.PrepareForSave(_dt, tableName, autoIncrementKey, primaryKey);
}
```

Caller shape: `rs.InitUpdate("species", autoIncrementKey: true, "speciesid")` for serial/identity
tables; existing `InitUpdate(table, keys...)` signature unchanged for explicit-key tables.

## Behavior matrix

| Scenario | Today | After |
|---|---|---|
| SQL Server, update | works | unchanged (mixed flags -> heuristic doesn't fire) |
| SQL Server, identity insert | works | unchanged; `autoIncrementKey: true` is a no-op |
| PostgreSQL, update | `ReadOnlyException` | fixed (blanket detected, cleared) |
| PostgreSQL, serial insert | `NoNullAllowedException` | fixed with `autoIncrementKey: true` |
| Explicit-key tables (UUID, composite) | works where flags allow | keys writable and included in INSERT — correct |
| SQL Server computed column via `SELECT *` | protected (`ReadOnly`) | still protected — the reason the all-read-only heuristic exists |

## Settled design decisions

1. **All-columns-read-only heuristic**, not an unconditional clear. Unconditional would strip
   `ReadOnly` from SqlClient-flagged computed columns and the generated UPDATE would try to SET
   them — a SQL Server regression. A table where literally every column is read-only is useless
   for the declared write intent, so it is unambiguously the Npgsql blanket. Cost: conditional
   behavior; needs one clear sentence in the docs.
2. **Explicit `autoIncrementKey` parameter**, no inference from "single int PK". Inference would
   silently corrupt tables with natural (client-assigned) integer keys, and
   `DataColumn.AutoIncrement` cannot be set on non-integer columns (UUID keys throw).
3. **Static helper on DataTableAdapter**, so the raw `FillDataTable` -> `SaveChanges` flow gets a
   one-liner too; Recordset stays a thin delegate.
4. `AllowDBNull` is accurate NOT NULL metadata on both providers — never touched.

## Compat

No behavior change on SQL Server (heuristic never fires on mixed schemas; `AutoIncrement` already
set). On PostgreSQL, previously-throwing flows start working. Existing
`InitUpdate(string, params string[])` signature untouched. New public surface:
`DataTableAdapter.PrepareForSave` (2 overloads) + 1 `InitUpdate` overload.

## When implementing

- Replace the test helpers with the new API — `PrepareForSave` in
  `Integration/DataTableAdapterTests.cs` and `NormalizeSchemaFlags` in
  `Integration/RecordsetUpdateTests.cs` become regression tests for it. Add a case asserting
  SqlClient-style mixed schemas are left untouched (computed-column protection).
- Update docs: replace the manual flag-surgery snippets in docs/data-table-adapter.md
  ("Provider note" under the runtime-schema section) and docs/recordset.md (identity bullet
  under Updating) with the one-liner.
- Verify against both live containers (`docker compose up -d --wait`, host ports 1434/5433).
