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

### How Expressions Are Translated

Lambda filters go through a three-stage pipeline: a **partial evaluator** folds every parameter-independent subexpression down to a value, an **expression translator** walks what remains into a small SQL AST, and a **dialect-aware text generator** renders that AST to SQL text plus an ordered parameter list. The upshot for callers: you write ordinary C#, and anything that does not depend on the lambda's parameter -- method calls, indexers, static members, arithmetic on locals, nested property chains -- is evaluated client-side automatically, no matter how it's spelled.

```csharp
// All of these work with no local-variable hoisting:
var animal = await adapter.GetOne(a => a.OwnerId == Guid.Parse(idText));
var recent = await adapter.GetOne(a => a.Created > DateTime.Now.AddDays(-7));
await adapter.Fill(animals, a => a.Name == lookup[key].DisplayName);
```

Only subexpressions that *reference the lambda parameter* are translated to SQL; everything else is "just C#" and runs once, before the query is built. If the translator encounters a construct on the parameter side that has no SQL equivalent -- an arbitrary method with no registered translation, for example -- it throws `SqlExpressionException` naming the offending subexpression and the reason it cannot be translated (e.g., `Cannot translate expression 'a.Name.PadLeft(5)': method 'String.PadLeft' has no SQL translation.`). There is no silent client-side fallback for the parameter side: an expression either translates to SQL or the call throws before anything executes.

### Supported Expression Surface

| C# expression | SQL emitted | Notes |
|---|---|---|
| `==  !=  <  <=  >  >=` | `=  !=  <  <=  >  >=` | All six comparison operators. |
| `&&`&nbsp;&nbsp;`\|\|`&nbsp;&nbsp;`!` | `AND`&nbsp;&nbsp;`OR`&nbsp;&nbsp;`NOT` | Parenthesized by precedence rank; nesting is always correct, never over- or under-parenthesized. |
| `+  -  *  /  %`, unary `-` | `+  -  *  /  %`, unary `-` | Arithmetic on numeric columns, e.g. `e.Capacity * 2 - 1 >= 9` → `(((Capacity * $0) - $1) >= $2)`. |
| `a.Notes ?? "none"` | `COALESCE(Notes, $0)` | `??` on a nullable column. |
| `cond ? x : y` | `CASE WHEN cond THEN x ELSE y END` | Ternary. |
| `a.NullableId.HasValue` | `IS NOT NULL` | See the [migration note](#pre-v70-behavior-changes) below -- the legacy parser emitted this backwards. |
| `a.NullableId.Value` | (bare column) | Unwraps; the column itself already carries the value. |
| `a.BoolColumn` (predicate position) | `BoolColumn = 1` | Bare bool column used as a condition (root, or operand of `AND`/`OR`/`NOT`). PostgreSQL renders the bare column instead: `(IsEndangered)`. Controlled by dialect via `FormatUnaryBoolean`. |
| `a.BoolColumn` (value position) | bare column | E.g. as the argument of another comparison. |
| `a.BoolColumn == true` | `BoolColumn = $0` | Explicit comparison, parameterized normally. |
| `a => true` / `a => false` | `1 = 1` / `1 = 0` | Constant predicates. |
| `a.Name.StartsWith(s)` / `EndsWith(s)` / `Contains(s)` | `Name LIKE $0` (with `%`/`_` positioned and the *value* wildcard-escaped) | See [Wildcards and case-insensitivity](#wildcards-and-case-insensitivity). |
| `...With(s, StringComparison.OrdinalIgnoreCase)` (also `InvariantCultureIgnoreCase`, `CurrentCultureIgnoreCase`) | PostgreSQL: `Name ILIKE $0`; other dialects: `UPPER(Name) LIKE UPPER($0)` | Case-sensitive `StringComparison` values (`Ordinal`, etc.) behave like the plain overload. |
| `a.Name.ToUpper()` / `ToLower()` (on a column) | `UPPER(Name)` / `LOWER(Name)` | On a captured value instead, folds client-side (stage 1). |
| `a.Name.Trim()` | `TRIM(Name)` | |
| `a.Name.Length` | `LENGTH(Name)` (SQL Server: `LEN([Name])`) | |
| `a.Name.Substring(start, len)` | `SUBSTRING(Name FROM $0 FOR $1)` | 0-based C# `start` becomes a 1-based SQL parameter. |
| `a.Name.Substring(start)` | `SUBSTRING(Name FROM $0)` | Same 1-based adjustment. |
| `a.Name.IndexOf(s)` | `(POSITION($0 IN Name) - 1)` | 0-based, matching C# `IndexOf` semantics. |
| `a.Name.Replace(a, b)` | `REPLACE(Name, $0, $1)` | |
| `string.IsNullOrEmpty(a.Notes)` | `(Notes IS NULL OR Notes = '')` | |
| `a.Name.Equals(s)` / `string.Equals(a.Name, s)` | `Name = $0` | Instance and static forms both translate. |
| `a.Name.Equals(s, StringComparison.OrdinalIgnoreCase)` | `UPPER(Name) = UPPER($0)` | |
| `a.Name.SqlLike(pattern)` | `Name LIKE $0` | Raw pattern, passed through **unescaped** -- caller owns `%`/`_`. |
| `a.Name.SqlILike(pattern)` | PostgreSQL: `Name ILIKE $0`; other dialects: `UPPER(Name) LIKE UPPER($0)` | Case-insensitive raw pattern. |
| `list.Contains(a.Field)` | `Field IN (...)` | See [IN Clauses](#in-clauses) for the parameterize/inline policy. |
| `a.Field.SqlIn(...)` (subquery overloads) | `Field IN (SELECT ... FROM ... WHERE ...)` | See [Subquery SqlIn](#subquery-sqlin). |
| `a.DateColumn.Value.Year` / `.Month` / `.Day` / `.Hour` / `.Minute` / `.Second` | ANSI: `EXTRACT(YEAR FROM DateColumn)`; SQL Server: `DATEPART(year, [DateColumn])`; SQLite: `CAST(strftime('%Y', [DateColumn]) AS INTEGER)` | Each date part follows the same per-dialect pattern. |
| `a.DateColumn.Value.Date` | `CAST(DateColumn AS DATE)` | |
| `Math.Abs(x)` / `Floor(x)` / `Ceiling(x)` | `ABS(x)` / `FLOOR(x)` / `CEILING(x)` | |
| `Math.Round(x)` / `Math.Round(x, n)` | `ROUND(x)` / `ROUND(x, $0)` | |
| `Regex.IsMatch(a.Field, pattern)` | PostgreSQL only: `(Field ~ $0)` | Other dialects throw `SqlExpressionException` naming the limitation. |
| `Regex.IsMatch(a.Field, pattern, RegexOptions.IgnoreCase)` | PostgreSQL only: `(Field ~* $0)` | Other `RegexOptions` values throw. |

### Values Come From Anywhere

Any subexpression that does not reference the lambda parameter is evaluated client-side before translation -- method calls, indexers, static members and fields, `DateTime.Now.AddDays(-7)`, nested property paths through captured objects, arithmetic on locals. There is no longer a need to hoist values into locals first:

```csharp
// All fold to a single client-side value, then translate as a normal parameter:
await adapter.Fill(animals, a => a.Created > DateTime.Now.AddDays(-7));
await adapter.Fill(animals, a => a.OwnerId == config.DefaultOwnerId);
await adapter.Fill(animals, a => a.Weight > weights[index]);
```

The one exception is `DateTime.Now`/`UtcNow`/`Today`: these are evaluated **on the client at translation time**, not translated to `GETDATE()` or an equivalent server function. A filter built at 09:00 and executed a minute later still compares against 09:00.

On modern targets (net8.0+, C# 14+), `array.Contains(x)` binds to the span-based `MemoryExtensions.Contains` overload rather than `Enumerable.Contains`; Zonkey translates it the same way, with the same IN semantics -- no special handling required.

### Wildcards and case-insensitivity

- Wildcard characters (`%`, `_`) in the *value* passed to `StartsWith`/`EndsWith`/`Contains` are escaped automatically, so a value containing a literal `%` or `_` matches literally rather than as a wildcard. Use `SqlLike`/`SqlILike` when you want to write a raw pattern with caller-controlled wildcards.
- Case-insensitive matching has two surfaces: the BCL `StringComparison` overloads of `StartsWith`/`EndsWith`/`Contains`/`Equals` (`*IgnoreCase` values), and the `SqlLike`/`SqlILike` marker methods. Both render PostgreSQL `ILIKE` and `UPPER(x) LIKE UPPER(y)` (or `UPPER(x) = UPPER(y)` for `Equals`) elsewhere.
- **These are database/collation approximations, not CLR semantics.** `StringComparison.*IgnoreCase` values (`Ordinal`, `InvariantCulture`, `CurrentCulture`, all case-insensitive) all translate the same way -- `UPPER(x) LIKE UPPER(y)` (PostgreSQL: `ILIKE`) -- there is no distinct handling per comparison kind. Case-sensitive values (`Ordinal`, etc.) emit a plain `LIKE`/`=` and defer entirely to the column's collation. Neither path reproduces .NET's ordinal or culture-aware comparison rules exactly; results can differ from the in-memory C# comparison for non-ASCII text, Turkish-I-style casing, or collations that are themselves case-insensitive or accent-insensitive.
- **Indexing caveat:** `ILIKE` and `UPPER(col)` both defeat a plain b-tree index. If case-insensitive search on a large table needs to be fast, that's a schema decision, not something the translator can paper over -- add a trigram index (PostgreSQL `pg_trgm`) or a computed/persisted uppercase column with its own index (SQL Server), matching how you query.

### Correlated subqueries are not supported

The subquery `SqlIn` where-lambda (see [Subquery SqlIn](#subquery-sqlin)) cannot reference the *outer* lambda's parameter -- there is no correlated-subquery support. Referencing the outer parameter inside the subquery's filter currently fails with an unhelpful exception rather than a clear "not supported" message. Keep subquery filters self-contained to the inner table (plus captured locals, which fold normally).

### Pre-v7.0 behavior changes

If you're upgrading from an earlier Zonkey release, two things changed:

- **`HasValue` now emits `IS NOT NULL`.** The legacy string-based parser emitted this inverted (a bug); code that happened to compensate for the old behavior will need to drop that compensation.
- **Empty `Contains`/`list.Contains(field)` now yields `1 = 0`** (no rows) instead of throwing. The obsolete `SqlIn`/`SqlInInt`/`SqlInGuid` methods keep throwing `ArgumentException` on an empty list, unchanged, for backward compatibility.

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

The idiomatic form is a plain LINQ `Contains` call on any in-scope list, array, or `IEnumerable<T>` inside a lambda expression -- no extension method needed:

```csharp
var categoryList = new[] { "shirts", "hoodies", "accessories" };
await adapter.Fill(products, p => categoryList.Contains(p.Category));

var ids = new List<int> { 1, 2, 3, 42 };
await adapter.Fill(products, p => ids.Contains(p.Id));
```

### Inlining policy

The translator decides automatically whether values are parameterized or inlined as literals -- there is no type-specific method to choose between:

- **64 values or fewer:** one command parameter per value, regardless of type.
- **More than 64 values of an injection-safe literal type** (`byte`, `short`, `int`, `long`, `Guid`): inlined directly into the SQL text as literals, avoiding the parameter-count limit. Enums are *not* inlined this way -- see the note below.
- **More than 64 values of any other type** (strings, dates, enums): stay parameterized. Past the dialect's parameter limit (2,100 for SQL Server) this throws `SqlExpressionException` with a hint to use `SplitList`.
- **Empty list:** renders `1 = 0` (matches no rows) rather than throwing.

Enum values are always sent as parameters, never inlined as literals, even in large lists -- this keeps PostgreSQL's natively-mapped enum columns working, since the provider (not the translator) decides the enum's wire representation.

### Subquery SqlIn

For `IN (SELECT ...)` against another mapped type, use the `SqlIn` lambda overloads. These remain the supported, non-obsolete way to express a subquery:

```csharp
using Zonkey.Extensions;

// 3-arg form: explicit select field + filter
await adapter.Fill(animals, a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen));
// => ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (IsOpen = 1))

// 2-arg form: select field name is inferred from the outer member
await adapter.Fill(animals, a => a.ExhibitId.SqlIn((Exhibit e) => e.Capacity > 50));
// => ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (Capacity > $0))
```

The subquery's filter lambda can reference its own parameter and captured locals, but **cannot reference the outer lambda's parameter** -- correlated subqueries are not supported (see [Correlated subqueries are not supported](#correlated-subqueries-are-not-supported)). Subquery parameters share numbering with the outer clause's parameters, and `NoLock` is honored on dialects that support it (`WITH (NOLOCK)` on SQL Server).

### Legacy `SqlIn`/`SqlInInt`/`SqlInGuid`

The original `SqlIn(IEnumerable)`, `SqlInInt`, and `SqlInGuid` extension methods are `[Obsolete]` as of v7.0 in favor of `list.Contains(field)`, which now covers everything they did (parameterized or inlined, chosen automatically) plus types they never supported. They still translate and still work -- existing code is not broken -- but two legacy quirks are preserved intentionally for backward compatibility rather than adopting the new `Contains` behavior:

- **An empty list still throws `ArgumentException`**, where `list.Contains(field)` now renders `1 = 0`.
- `SqlInInt`/`SqlInGuid` **always inline as literals**, regardless of list size, matching their original semantics.

New code should prefer `list.Contains(field)`.

### Large Lists with SplitList

For large lists of strings or dates (types that stay parameterized past 64 items and hit the 2,100-parameter limit), use `SplitList` to break them into batches:

```csharp
using Zonkey.Extensions;

var allNames = GetLargeNameList(); // thousands of names
var results = new List<Product>();

foreach (var chunk in allNames.SplitList(2000))
{
    await adapter.Fill(results, p => chunk.Contains(p.Name));
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

- Use **lambda expressions** when you want compile-time type safety. Values can come from anywhere in scope (see [Values Come From Anywhere](#values-come-from-anywhere)), but the [supported expression surface](#supported-expression-surface) is not the full C# language -- untranslatable constructs throw `SqlExpressionException` rather than falling back to client evaluation. You work in property names.
- Use **SqlFilter** when you need to build filters conditionally at runtime (e.g., optional search parameters from a user interface or API). You work in database column names.
- Use **string filters** when you need database-specific SQL syntax or complex expressions that lambda parsing does not support.

---

## See Also

- [data-class-adapter.md](data-class-adapter.md) -- Full adapter API reference (save, delete, bulk operations, events)
