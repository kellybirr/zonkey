# DataManager — Raw SQL Operations

DataManager provides direct SQL execution for scenarios where mapped objects are not appropriate: ad-hoc queries, aggregations, DDL statements, stored procedures, and other operations that do not map cleanly to a single entity type.

## Overview

```csharp
var dm = new DataManager(connection);
```

DataManager wraps a `DbConnection` and provides parameterized SQL execution. It auto-detects the SQL dialect from the connection type when the `Connection` property is set.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Connection` | `DbConnection` | The database connection. Setting this also auto-detects `SqlDialect`. |
| `SqlDialect` | `SqlDialect` | Auto-detected from the connection type. Can be overridden. |
| `CommandTimeout` | `int?` | Per-instance command timeout. |
| `ParameterPrefix` | `char` | Parameter placeholder prefix (default: `'$'`). |

**Static properties:**

| Property | Type | Description |
|---|---|---|
| `DefaultCommandTimeout` | `int?` | Default timeout applied to newly created DataManager instances. |

## Scalar Queries

```csharp
// Simple scalar
object result = await dm.ExecuteScalar("SELECT COUNT(*) FROM products");

// With parameters
object result = await dm.ExecuteScalar(
    "SELECT MAX(price) FROM products WHERE category = $0", "shirts");

// Stored procedure (isProc = true)
object result = await dm.ExecuteScalar("get_product_count", true, "shirts");
```

Returns the first column of the first row in the result set. Parameters use positional placeholders (`$0`, `$1`, `$2`...).

**Overloads:**

```csharp
Task<object> ExecuteScalar(string sql)
Task<object> ExecuteScalar(string sql, params object[] parameters)
Task<object> ExecuteScalar(string sql, bool isProc, params object[] parameters)
Task<object> ExecuteScalar(DbCommand command)
```

## Non-Query Operations

```csharp
// With parameters
int affected = await dm.ExecuteNonQuery(
    "UPDATE products SET price = price * 1.1 WHERE category = $0", "shirts");

// No parameters
int affected = await dm.ExecuteNonQuery("DELETE FROM expired_sessions");

// Stored procedure (isProc = true)
int affected = await dm.ExecuteNonQuery("archive_old_orders", true, cutoffDate);

// From a DbCommand
int affected = await dm.ExecuteNonQuery(myCommand);
```

**Overloads:**

```csharp
Task<int> ExecuteNonQuery(string sql)
Task<int> ExecuteNonQuery(string sql, params object[] parameters)
Task<int> ExecuteNonQuery(string sql, bool isProc, params object[] parameters)
Task<int> ExecuteNonQuery(DbCommand command)
```

## Data Readers

```csharp
using var reader = await dm.GetDataReader(
    "SELECT id, name, price FROM products WHERE category = $0",
    CommandType.Text, "shirts");

while (await reader.ReadAsync())
{
    var id = reader.GetInt32(0);
    var name = reader.GetString(1);
    var price = reader.GetDecimal(2);
}
```

**Signature:**

```csharp
Task<DbDataReader> GetDataReader(string sql, CommandType commandType, params object[] parameters)
```

## DataSet and DataTable

```csharp
// Get a DataSet (supports multiple result sets)
DataSet ds = await dm.GetDataSet(
    "SELECT * FROM products; SELECT * FROM categories",
    CommandType.Text);

// Fill an existing DataTable
var dt = new DataTable();
await dm.FillDataTable(dt,
    "SELECT * FROM products WHERE price > $0",
    CommandType.Text, 10.0m);
```

**Signatures:**

```csharp
Task<DataSet> GetDataSet(string sql, CommandType commandType, params object[] parameters)
Task FillDataTable(DataTable dt, string sql, CommandType commandType, params object[] parameters)
```

## Parameter Handling

Parameters are dialect-aware. The `ParameterPrefix` (default `'$'`) marks positional placeholders that are replaced with dialect-appropriate parameter names at execution time:

```csharp
// Positional parameters ($0, $1, $2...)
await dm.ExecuteNonQuery(
    "INSERT INTO audit_log (entity, action, timestamp) VALUES ($0, $1, $2)",
    "Product", "updated", DateTime.UtcNow);
```

You can also pass `DbParameter` objects directly for full control. They are added to the command as-is:

```csharp
var param = new NpgsqlParameter("@name", NpgsqlDbType.Text) { Value = "Classic Tee" };
await dm.ExecuteNonQuery("UPDATE products SET name = @name WHERE id = $0", param, 42);
```

Static helpers for adding parameters to commands:

```csharp
var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM products WHERE category = $0";
DataManager.AddParamsToCommand(command, dialect, new object[] { "shirts" });
```

**AddParamsToCommand overloads:**

```csharp
// Instance method (uses instance dialect and prefix)
void AddParamsToCommand(DbCommand command, IList parameters)
void AddParamsToCommand(DbCommand command, IList parameters, char placeholderPrefix)

// Static methods
static void AddParamsToCommand(DbCommand command, SqlDialect dialect, IList parameters)
static void AddParamsToCommand(DbCommand command, SqlDialect dialect, IList parameters, char placeholderPrefix)
```

## Stored Procedures

```csharp
// Via Execute methods (isProc = true)
var result = await dm.ExecuteScalar("calculate_order_total", true, orderId);

// Via GetCommandFromSP helper
var cmd = dm.GetCommandFromSP("get_customer_orders", customerId, startDate);
using var reader = await cmd.ExecuteReaderAsync();
```

`GetCommandFromSP` creates a `DbCommand` with `CommandType.StoredProcedure`, adds parameters, sets the command timeout, and enrolls the command in any registered transaction.

## When to Use DataManager

Use DataManager instead of DataClassAdapter when:

- Running aggregate queries (`SUM`, `COUNT`, `AVG`) that do not map to an entity
- Executing DDL statements (`CREATE`, `ALTER`, `DROP`)
- Calling stored procedures with complex output
- Running batch operations or multi-statement SQL
- Working with queries that return data from multiple tables in a non-entity shape
- You need direct control over the SQL

For entity-shaped results, prefer [DataClassAdapter](data-class-adapter.md). For raw results, use DataManager.

## See Also

- [DatabaseWrapper](database-wrapper.md) — DataManager is often exposed as a property on your wrapper class
- [Async Patterns](async-patterns.md) — async best practices with Zonkey
