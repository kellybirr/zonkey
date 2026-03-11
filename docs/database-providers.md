# Database Providers & Dialects

Zonkey uses a pluggable dialect system to generate correct SQL for each database engine. The dialect is auto-detected from the `DbConnection` type -- no manual configuration is needed in most cases.

---

## Overview

When you assign a `DbConnection` to a `DataClassAdapter`, `DataTableAdapter`, or `DataManager`, Zonkey inspects the connection's runtime type name and selects the appropriate `SqlDialect` subclass. Each dialect controls how identifiers are quoted, how parameters are named, how pagination works, and how auto-increment values are retrieved.

---

## Supported Databases

| Database | Connection Type | Dialect | Notes |
|----------|----------------|---------|-------|
| SQL Server | `Microsoft.Data.SqlClient.SqlConnection` | `SqlServerDialect` | Full support: NOLOCK, row version, schema, change tracking, TOP/ROW_NUMBER |
| SQL Server | `System.Data.SqlClient.SqlConnection` | `SqlServerDialect` | Legacy provider, same dialect |
| SQL Server CE | `System.Data.SqlServerCe.SqlCeConnection` | `SqlServerDialect` | Compact edition |
| PostgreSQL | `Npgsql.NpgsqlConnection` | `PostgreSqlDialect` | LIMIT/OFFSET, sequences, ILIKE, regex operators |
| MySQL | `MySql.Data.MySqlClient.MySqlConnection` | `MySqlDialect` | LIMIT/OFFSET, backtick identifiers, schema support |
| MySQL (Devart) | `Devart.Data.MySql.MySqlConnection` | `MySqlDialect` | Third-party provider |
| MySQL (CoreLab) | `CoreLab.MySql.MySqlConnection` | `MySqlDialect` | Legacy provider |
| Oracle | `System.Data.OracleClient.OracleConnection` | `OracleSqlDialect` | Based on ANSI dialect, sequence support |
| DB2 | `IBM.Data.DB2.DB2Connection` | `DB2SqlDialect` | Positional parameters for text commands |
| SQLite | `Mono.Data.Sqlite.SqliteConnection` | `SqliteDialect` | LIMIT/OFFSET, GUID-to-string parameter handling |
| Access | (OleDb) | `AccessSqlDialect` | TOP 1 optimization, bracket identifiers, positional parameters |
| Any (fallback) | Any unrecognized | `GenericSqlDialect` | Minimal, positional parameters only |

---

## Dialect Auto-Detection

When you set the `Connection` property on any adapter, the dialect is determined automatically by looking up the connection type's full name in `SqlDialect.Factories`:

```csharp
// Dialect auto-detected as PostgreSqlDialect
var adapter = new DataClassAdapter<Product>(npgsqlConnection);

// Dialect auto-detected as SqlServerDialect
var adapter = new DataClassAdapter<Product>(sqlConnection);
```

You can override the dialect after construction if needed:

```csharp
adapter.SqlDialect = new Zonkey.Dialects.PostgreSqlDialect();
```

---

## What Dialects Customize

Each dialect controls the following behaviors:

- **Identifier quoting**: brackets `[name]` (SQL Server, SQLite, Access), backticks `` `name` `` (MySQL, optional), double quotes `"name"` (PostgreSQL/ANSI/Oracle/DB2)
- **Parameter naming**: `@p0` (SQL Server, SQLite), `:p0` (PostgreSQL/ANSI/Oracle), `?` (positional-only for DB2 text commands, Access, Generic)
- **Pagination**: `TOP n` / `ROW_NUMBER()` (SQL Server), `LIMIT n OFFSET m` (PostgreSQL, SQLite), `LIMIT start,length` (MySQL)
- **Identity retrieval**: `SCOPE_IDENTITY()` (SQL Server), `LAST_INSERT_ID()` (MySQL), `last_insert_rowid()` (SQLite), `lastval()`/`currval('seq')` (PostgreSQL, Oracle), `SYSIBM.IDENTITY_VAL_LOCAL()` (DB2), `@@IDENTITY` (Access)
- **Single-row optimization**: `SELECT TOP 1` (SQL Server, Access), `LIMIT 1` (PostgreSQL), `LIMIT 0,1` (MySQL)
- **String operations**: how `StartsWith`, `EndsWith`, and `Contains` map to `LIKE` expressions with database-specific concatenation (`+` for SQL Server, `CONCAT()` for MySQL/ANSI, `||` for SQLite, `&` for Access)
- **Feature flags**: NOLOCK support (`SupportsNoLock`), row version support (`SupportsRowVersion`), stored procedures (`SupportsStoredProcedures`), batch SQL (`UseSqlBatches`), schema support (`SupportsSchema`), change tracking context (`SupportsChangeContext`), named parameters (`UseNamedParameters`), LIMIT support (`SupportsLimit`)
- **Parameter fixing**: database-specific parameter adjustments (e.g., SQLite converts `DbType.Guid` to `DbType.String`, PostgreSQL converts enum values to their underlying integer types)

---

## Registering Custom Dialects

The `SqlDialect.Factories` dictionary maps connection type names to factory functions. You can register additional connection types at application startup:

```csharp
SqlDialect.Factories["My.Custom.DbConnection"] = conn => new MyCustomDialect();
```

The lookup is case-insensitive. If no match is found, `GenericSqlDialect` is used as a fallback.

---

## SQL Server Extensions (Zonkey.Data.MsSql)

The optional `Zonkey.Data.MsSql` package adds:

- `Microsoft.Data.SqlClient` provider factory registration (for environments where it is not auto-registered)
- Proper `SqlDbType.Time` parameter handling via `DbParameterExtensions`
- `SqlXmlAdapter` for working with SQL Server XML query results

Install:

```shell
dotnet add package Zonkey.Data.MsSql
```

Initialize at startup:

```csharp
MsSqlExtension.Initialize();
```

### SqlXmlAdapter

`SqlXmlAdapter` executes FOR XML queries and returns the results as `XmlDocument` or string:

```csharp
using Zonkey.SqlServer;

var xmlAdapter = new SqlXmlAdapter(sqlConnection);

// Get results as XmlDocument
XmlDocument xmlDoc = await xmlAdapter.GetXmlDocument("root",
    "SELECT * FROM products FOR XML PATH('product')", false);

// Fill an existing XmlNode
int count = await xmlAdapter.FillXmlNode(rootNode,
    "SELECT * FROM products FOR XML PATH('product')", false);

// Get raw XML string
string xml = await xmlAdapter.GetXmlString(
    "SELECT * FROM products FOR XML PATH('product')", false);
```

The `bool isProc` parameter indicates whether the SQL text is a stored procedure name (`true`) or an ad-hoc query (`false`).

---

## Database-Specific Features

### SQL Server NOLOCK

The `NoLock` property on `DataClassAdapter<T>` adds `WITH (NOLOCK)` hints to SELECT queries. It is only applied when the dialect supports it (`SqlServerDialect`):

```csharp
var adapter = new DataClassAdapter<Product>(connection);
adapter.NoLock = true;
await adapter.Fill(products, p => p.Category == "shirts");
// Generates: SELECT ... FROM [products] WITH (NOLOCK) WHERE ...
```

### SQL Server Change Tracking Context

SQL Server supports change tracking context for auditing. The `SqlServerDialect` prepends `WITH CHANGE_TRACKING_CONTEXT(...)` to commands when a context object is provided.

### PostgreSQL ILIKE and Regex

PostgreSQL-specific filter operators are available through `SqlFilter`:

```csharp
// Case-insensitive LIKE
adapter.Fill(products, SqlFilter.ILIKE("name", "%tee%"));

// Case-insensitive regex match (~*)
adapter.Fill(products, SqlFilter.IMATCH("name", "^classic.*"));

// Case-sensitive regex match (~)
adapter.Fill(products, SqlFilter.MATCH("name", "^Classic.*"));

// Negated variants
adapter.Fill(products, SqlFilter.NOTILIKE("name", "%sold out%"));
adapter.Fill(products, SqlFilter.NOTMATCH("name", "^test.*"));
adapter.Fill(products, SqlFilter.NOTIMATCH("name", "^draft.*"));
```

### PostgreSQL and Oracle Sequences

When using sequence-based auto-increment columns, specify the sequence name on the `DataField` attribute:

```csharp
[DataField("id", DbType.Int64, IsKeyField = true, IsAutoIncrement = true, SequenceName = "products_id_seq")]
public long Id { get => _id; set => SetFieldValue(ref _id, value); }
```

For `DataTableAdapter`, set the `SequenceName` property on the adapter instance.

---

## See Also

- [DataClassAdapter](data-class-adapter.md) -- typed CRUD operations
- [Getting Started](getting-started.md) -- initial setup and configuration
- [Back to README](../README.md)
