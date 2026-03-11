# Querying -- Filters, LINQ Expressions & Pagination

Zonkey provides three ways to express WHERE clauses for query operations:

- **Lambda expressions** -- type-safe, converted to SQL at runtime
- **SqlFilter** -- fluent, parameterized filter objects
- **String filters** -- raw SQL with positional parameters

Each approach is fully parameterized and injection-safe. All three work with `Fill`, `GetOne`, `GetCount`, `Exists`, `OpenReader`, `FillRange`, and `Delete`.

---

## Lambda Expressions

The most ergonomic option. Lambda expressions are parsed by `WhereExpressionParser` and converted to SQL WHERE clauses.

```csharp
// Simple comparison
await adapter.Fill(products, p => p.Price < 25.00m);

// Equality
var product = await adapter.GetOne(p => p.Id == 42);

// String methods
await adapter.Fill(products, p => p.Name.StartsWith("Classic"));
await adapter.Fill(products, p => p.Name.Contains("Tee"));

// Compound conditions
await adapter.Fill(products, p => p.Price >= 10.00m && p.Price <= 50.00m);
await adapter.Fill(products, p => p.Category == "shirts" || p.Category == "hoodies");

// Null checks
await adapter.Fill(products, p => p.Description != null);
```

**Important:** These are not `IQueryable` -- they only generate WHERE clauses. There is no query composition or deferred execution. The expression is evaluated once, a SQL command is generated, and the command is executed immediately.

---

## SqlFilter

Fluent, parameterized filter objects. Useful when building filters dynamically or when you need to construct queries from runtime conditions.

### Factory Methods

```csharp
SqlFilter.EQ("field", value)       // field = @p0
SqlFilter.NEQ("field", value)      // field != @p0
SqlFilter.GT("field", value)       // field > @p0
SqlFilter.GTE("field", value)      // field >= @p0
SqlFilter.LT("field", value)       // field < @p0
SqlFilter.LTE("field", value)      // field <= @p0
SqlFilter.LIKE("field", value)     // field LIKE @p0
SqlFilter.NOTLIKE("field", value)  // field NOT LIKE @p0
SqlFilter.NULL("field")            // field IS NULL
SqlFilter.NOTNULL("field")         // field IS NOT NULL
SqlFilter.NGT("field", value)      // field !> @p0
SqlFilter.NLT("field", value)      // field !< @p0
```

### PostgreSQL-Specific Operators

```csharp
SqlFilter.ILIKE("field", value)        // field ILIKE @p0 (case-insensitive LIKE)
SqlFilter.NOTILIKE("field", value)     // field NOT ILIKE @p0
SqlFilter.MATCH("field", regex)        // field ~ @p0 (regex match)
SqlFilter.NOTMATCH("field", regex)     // field !~ @p0
SqlFilter.IMATCH("field", regex)       // field ~* @p0 (case-insensitive regex)
SqlFilter.NOTIMATCH("field", regex)    // field !~* @p0
```

### Combining Filters

Multiple filters passed to a single method call are AND'd together:

```csharp
await adapter.Fill(products,
    SqlFilter.GT("price", 10.0m),
    SqlFilter.LT("price", 50.0m),
    SqlFilter.NOTNULL("description")
);
// WHERE price > @p0 AND price < @p1 AND description IS NOT NULL
```

### Dynamic Filter Building

Build a filter array at runtime based on conditions:

```csharp
var filters = new List<SqlFilter>();

if (minPrice.HasValue)
    filters.Add(SqlFilter.GTE("price", minPrice.Value));
if (maxPrice.HasValue)
    filters.Add(SqlFilter.LTE("price", maxPrice.Value));
if (!string.IsNullOrEmpty(category))
    filters.Add(SqlFilter.EQ("category", category));

await adapter.Fill(products, filters.ToArray());
```

---

## String Filters

Raw SQL WHERE clauses with positional parameter placeholders (`$0`, `$1`, `$2`...):

```csharp
// Single parameter
await adapter.Fill(products, "price > $0", 10.0m);

// Multiple parameters
await adapter.Fill(products, "price BETWEEN $0 AND $1", 10.0m, 50.0m);

// No parameters (literal SQL)
await adapter.Fill(products, "price > 0");
```

The dollar-sign placeholders are converted to dialect-appropriate parameter syntax (`@p0` for SQL Server, `:p0` for PostgreSQL, etc.). The default prefix is `$` and can be changed via `adapter.ParameterPrefix`.

String filters are parameterized and injection-safe when you use the placeholder syntax. Avoid concatenating user input directly into the filter string.

---

## IN Clauses

Use `SqlIn` extension methods within lambda expressions to generate SQL `IN (...)` clauses.

```csharp
using Zonkey.Extensions;

var categoryList = new[] { "shirts", "hoodies", "accessories" };
await adapter.Fill(products, p => p.Category.SqlIn(categoryList));
```

### Type-Specific Variants

For better performance with numeric and GUID types, use the type-specific methods:

```csharp
using Zonkey.Extensions;

// Integer IN clause (works with Int16, Int32, Int64 and their nullable equivalents)
var ids = new[] { 1, 2, 3, 42 };
await adapter.Fill(products, p => p.Id.SqlInInt(ids));

// GUID IN clause
var guids = new[] { guid1, guid2, guid3 };
await adapter.Fill(items, i => i.ExternalId.SqlInGuid(guids));
```

### Large Lists with SplitList

For large lists that may exceed database parameter limits, use `SplitList` to break them into batches:

```csharp
using Zonkey.Extensions;

var allIds = GetLargeIdList(); // thousands of IDs
var results = new List<Product>();

foreach (var chunk in allIds.SplitList(2000))
{
    await adapter.Fill(results, p => p.Id.SqlInInt(chunk));
}
```

`SplitList` is an extension method on `IEnumerable<T>` that returns `IList<IList<T>>`. The default batch size is 2000.

---

## Pagination -- FillRange

Load a page of results. The `OrderBy` property must be set before calling `FillRange`.

```csharp
adapter.OrderBy = "name ASC";

var page = new List<Product>();

// Get items 21-40 (skip 20, take 20)
await adapter.FillRange(page, 20, 20, p => p.Price > 0);

// Also works with SqlFilter
await adapter.FillRange(page, 0, 50, SqlFilter.EQ("category", "shirts"));

// And with string filters
await adapter.FillRange(page, 0, 50, "category = $0", "shirts");
```

The `FillRange` parameters are:

| Parameter | Type | Description |
|-----------|------|-------------|
| `collection` | `ICollection<T>` | The collection to populate |
| `start` | `int` | The zero-based row index to start from (number of rows to skip) |
| `length` | `int` | The maximum number of rows to return |
| filter | varies | Lambda expression, SqlFilter array, or string filter |

`FillRange` generates dialect-appropriate pagination SQL. The adapter checks `SqlDialect.SupportsLimit` and throws `InvalidOperationException` if the current dialect does not support pagination.

---

## Ordering

Set the `OrderBy` property before `Fill` or `FillRange` operations:

```csharp
adapter.OrderBy = "price DESC";
await adapter.Fill(products, p => p.Category == "shirts");

adapter.OrderBy = "name ASC, price DESC";
await adapter.FillAll(products);
```

`OrderBy` is required for `FillRange` and optional for `Fill`. When set, the ORDER BY clause is appended to the generated SELECT statement.

---

## Choosing the Right Approach

| Approach | Best for | Trade-off |
|----------|----------|-----------|
| Lambda expressions | Type-safe, simple conditions | No query composition; expression parsing overhead |
| SqlFilter | Dynamic filter building, API parameters | Slightly more verbose |
| String filters | Complex SQL, database-specific syntax | No compile-time checking |

All three approaches are parameterized and injection-safe. Choose based on readability and your use case.

- Use **lambda expressions** when you want compile-time type safety and the conditions are straightforward.
- Use **SqlFilter** when you need to build filters conditionally at runtime (e.g., optional search parameters from a user interface or API).
- Use **string filters** when you need database-specific SQL syntax or complex expressions that lambda parsing does not support.

---

## See Also

- [data-class-adapter.md](data-class-adapter.md) -- Full adapter API reference (save, delete, bulk operations, events)
