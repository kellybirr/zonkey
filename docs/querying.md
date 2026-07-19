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

### Expression Parser Limitations

The parser translates the expression tree to SQL -- it does not execute C# code. Three rules follow from this:

1. **No method calls inside the lambda.** The parser can read fields, properties, and captured local variables, but it cannot evaluate method calls -- `Guid.Parse(...)`, `DateTime.Now.AddDays(-7)`, `name.Trim()`, and anything similar throw `NotSupportedException`. Compute the value into a local variable first, then use the variable:

   ```csharp
   // Throws NotSupportedException:
   var animal = await adapter.GetOne(a => a.OwnerId == Guid.Parse(idText));

   // Works -- hoist the value into a local:
   var ownerId = Guid.Parse(idText);
   var animal = await adapter.GetOne(a => a.OwnerId == ownerId);
   ```

   The only method calls the parser understands are the `SqlIn` family (see [IN Clauses](#in-clauses)) and string predicate methods (`StartsWith`, `Contains`, `EndsWith`, ...), which translate to SQL.

2. **String methods depend on the dialect.** `StartsWith`/`Contains` translate through the dialect's function mapping; the real dialects (SQL Server, SQLite, PostgreSQL, MySQL, Access) all support them, but the generic fallback dialect does not and will throw.

3. **`SqlIn` needs a variable, a non-empty list, and at most 2,100 items.** Inline array literals (`p.X.SqlIn(new[]{...})`) and method-call results throw -- assign to a variable first. Empty lists throw `ArgumentException`. For large lists, see [SplitList](#large-lists-with-splitlist).

---

## SqlFilter

Fluent, parameterized filter objects. Useful when building filters dynamically or when you need to construct queries from runtime conditions.

**Field names, not property names.** `SqlFilter` (and string filters below) take database *column* names, which pass straight into the generated SQL. Lambda expressions are the only style that works in C# property names and maps them through the `DataField` attributes. With `[DataField("ProductID")] public int Id`, write `SqlFilter.EQ("ProductID", 42)` but `p => p.Id == 42`. Column-name quoting and case sensitivity vary by dialect -- see [Database Providers & Dialects](database-providers.md).

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

if (filters.Count > 0)
    await adapter.Fill(products, filters.ToArray());
else
    await adapter.FillAll(products);
```

Note the guard: passing an empty filter array throws `ArgumentNullException` -- there is no "no filters means all rows" fallback. Use `FillAll` when no conditions apply.

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

The dollar-sign placeholders are converted to dialect-appropriate parameter syntax (`@p0` for SQL Server, `:p0` for PostgreSQL, etc.). The placeholder number is the argument's zero-based position in the parameter list.

The prefix is `$` by default. `adapter.ParameterPrefix` exists but is best left alone: it only affects string-filter substitution, while the lambda parser always emits `$`-prefixed placeholders -- changing the prefix breaks lambda-based filters on the same adapter.

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

For better performance with numeric and GUID types, use the type-specific methods. Unlike `SqlIn`, which binds one command parameter per value (and is therefore subject to parameter-count limits like SQL Server's 2,100), `SqlInInt` and `SqlInGuid` emit the values as inline SQL literals -- safe because the types cannot carry injection payloads:

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

- Use **lambda expressions** when you want compile-time type safety and the conditions are straightforward. Remember the [parser limitations](#expression-parser-limitations): values must be pre-computed into locals, and you work in property names.
- Use **SqlFilter** when you need to build filters conditionally at runtime (e.g., optional search parameters from a user interface or API). You work in database column names.
- Use **string filters** when you need database-specific SQL syntax or complex expressions that lambda parsing does not support.

---

## See Also

- [data-class-adapter.md](data-class-adapter.md) -- Full adapter API reference (save, delete, bulk operations, events)
