# Migrating from Entity Framework

This guide maps Entity Framework (EF/EF Core) concepts to their Zonkey equivalents and explains the mental model shift. The two libraries have fundamentally different philosophies: EF optimizes for convenience through implicit behavior, while Zonkey optimizes for predictability through explicit behavior. Neither approach is wrong -- they serve different priorities.

## Concept Mapping

| EF Concept | Zonkey Equivalent | Key Difference |
|---|---|---|
| `DbContext` | `DatabaseWrapper` | No automatic change tracking. `DatabaseWrapper` manages a connection and caches adapters, but does not track entities. |
| `DbSet<T>` | `DataClassAdapter<T>` | Operations are on individual objects, not tracked sets. The adapter is a stateless bridge between objects and the database. |
| Entity class (POCO) | `DataClass` (with attributes) | `DataClass` adds optional per-object change tracking via `SetFieldValue`. For read-only scenarios, plain POCOs with `DataItem` and `DataField` attributes work without inheriting from `DataClass`. |
| `context.SaveChanges()` | `adapter.Save(entity)` | Saves exactly one object. No implicit cascade. You control what saves and when. |
| `.Include()` / `.ThenInclude()` | Manual loading | Load related data explicitly with separate queries. Join results in memory using standard LINQ. |
| Lazy loading | Not supported (by design) | All data loading is explicit. You never get surprise queries from accessing a navigation property. |
| `IQueryable` / LINQ-to-SQL | Lambda expressions + `SqlFilter` | Lambda expressions convert to WHERE clauses only. There is no query composition, projection, or server-side grouping through LINQ syntax. |
| Migrations | Manual or code generation tools | Schema management is separate from the ORM. Use database tools or the [code generation tools](code-generation.md). |
| `ChangeTracker` | `DataRowState` per object | Each object tracks its own state using `System.Data.DataRowState`, including `Added`, `Modified`, `Unchanged`, `Detached`, and `Deleted` (used by collection saves). There is no global tracker. You inspect state directly on the object. |
| `SaveChanges()` (batch) | `TrySaveCollection` / individual `Save` | You choose whether to save one item or iterate a collection. Each save operation is explicit and independent. |
| `CountAsync()` | `adapter.GetCount(...)` | Counts matching rows without loading objects. Returns `Task<long>`, not `int`. |
| `AnyAsync()` | `adapter.Exists(...)` | Existence check. As of v6.6 it generates portable SQL through the dialect system and works on all supported dialects. |

## Mental Model Shifts

### 1. "Register and forget" becomes "Load and manage"

In EF, you add entities to a context, modify them freely, and call `SaveChanges()` to persist everything the context has tracked. The context knows what changed.

In Zonkey, you explicitly call `Save` on each object you want to persist. There is no context accumulating changes. This means you always know what is being sent to the database, because you are the one sending it.

### 2. "The context knows" becomes "You know"

EF's `ChangeTracker` monitors all entities loaded through a context. It detects property changes, computes diffs, and batches operations automatically.

In Zonkey, each object tracks itself. When you modify a property through `SetFieldValue`, the object records the original value in its `OriginalValues` dictionary and sets its `DataRowState` to `Modified`. You can inspect an object's state at any time without needing a context reference.

```csharp
// You can check any object's state directly
if (product.DataRowState == DataRowState.Modified)
{
    Console.WriteLine($"Original price was: {product.OriginalValues["Price"]}");
}
```

### 3. "Navigation properties" becomes "Separate queries"

EF resolves related data through navigation properties, either eagerly (via `.Include()`) or lazily (via proxies). This is convenient but can produce unexpected queries, especially with lazy loading in loops.

In Zonkey, you load related data with separate, explicit queries. This is more verbose, but it means you never accidentally trigger N+1 queries by iterating a collection and accessing a navigation property.

### 4. "IQueryable builds server-side queries" becomes "Expressions build WHERE clauses"

EF's `IQueryable` lets you compose queries using LINQ syntax, with the entire expression tree translated to SQL on the server. This is powerful for dynamic query building.

Zonkey's lambda expression support converts expressions to SQL WHERE clauses, but there is no `IQueryable` pipeline. For simple filters, lambda expressions work well. For complex or dynamic queries, use `SqlFilter` objects or raw SQL through `DataManager`.

### 5. "Migrations manage schema" becomes "Schema is separate"

EF can generate and apply database migrations from model changes, keeping your code and schema in sync.

Zonkey does not manage your schema. Your data classes must match your database tables, but how you maintain that correspondence is up to you. Use database-native tools, third-party migration libraries, or the [code generation tools](code-generation.md) to generate classes from existing tables.

## Pitfall: Method Calls Inside Lambdas

This is the single most common EF habit that breaks. Zonkey's lambda support is a WHERE-clause translator, not LINQ-to-objects -- method calls inside the lambda are not evaluated, and the expression parser throws `NotSupportedException`:

```csharp
// Both throw NotSupportedException -- method calls are not translated
var recent = await adapter.GetOne(a => a.Created > DateTime.Now.AddDays(-7));
var item = await adapter.GetOne(a => a.Id == Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"));
```

EF providers evaluate these subexpressions client-side before translating the query; Zonkey does not. Compute the value into a local variable first and use the variable in the lambda:

```csharp
var cutoff = DateTime.Now.AddDays(-7);
var recent = await adapter.GetOne(a => a.Created > cutoff);

var id = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
var item = await adapter.GetOne(a => a.Id == id);
```

## Side-by-Side Examples

### Loading a Single Item

```csharp
// EF
var product = await context.Products.FirstOrDefaultAsync(p => p.Id == 42);

// Zonkey
var adapter = new DataClassAdapter<Product>(connection);
var product = await adapter.GetOne(p => p.Id == 42);
```

Both approaches generate similar SQL. The difference is that the EF version returns an entity tracked by the context, while the Zonkey version returns a standalone object.

### Loading Related Data

```csharp
// EF -- eager loading via Include
var order = await context.Orders
    .Include(o => o.OrderLines)
    .FirstOrDefaultAsync(o => o.Id == 100);

// Zonkey -- explicit, separate queries
var orderAdapter = new DataClassAdapter<Order>(connection);
var order = await orderAdapter.GetOne(o => o.Id == 100);

var lineAdapter = new DataClassAdapter<OrderLine>(connection);
var lines = new List<OrderLine>();
await lineAdapter.Fill(lines, ol => ol.OrderId == 100);
```

The Zonkey version requires two queries and two adapters. The tradeoff: you can see exactly what data is being loaded, control the timing, and avoid loading data you do not need.

### Saving Changes

```csharp
// EF -- context tracks everything
var product = await context.Products.FindAsync(42);
product.Price = 19.99m;
await context.SaveChangesAsync(); // saves ALL tracked changes across ALL entities

// Zonkey -- explicit, single-object save
var adapter = new DataClassAdapter<Product>(connection);
var product = await adapter.GetOne(p => p.Id == 42);
product.Price = 19.99m;
// product.DataRowState is now Modified
await adapter.Save(product); // saves THIS object only
```

In EF, `SaveChanges` persists every tracked change in the context. In Zonkey, `Save` persists a single object. If you have modified five objects and only want to save three, you call `Save` three times.

### Creating a New Record

```csharp
// EF
var product = new Product { Name = "Classic Tee", Price = 24.99m };
context.Products.Add(product);
await context.SaveChangesAsync();

// Zonkey
var product = new Product(addingNew: true) { Name = "Classic Tee", Price = 24.99m };
// product.DataRowState is Added
var adapter = new DataClassAdapter<Product>(connection);
await adapter.Save(product);
// product.Id is now populated from the database (if auto-increment)
```

In Zonkey, the `addingNew: true` constructor parameter sets `DataRowState` to `Added`, which tells the adapter to generate an INSERT statement when `Save` is called. After a successful insert, auto-increment and computed values are selected back into the object by default.

## When to Stay with Entity Framework

EF is a better fit when:

- **You need `IQueryable` composition for dynamic queries.** EF's LINQ-to-SQL provider enables complex, composable queries that translate entirely to server-side SQL. Zonkey does not offer this.
- **You rely on automatic schema migrations.** EF's migration system keeps your code and database in sync with minimal manual effort. Zonkey has no equivalent.
- **Your team is deeply invested in EF patterns and conventions.** Switching ORMs has a real learning curve cost. If EF is working well for your team and you understand its tradeoffs, there may be no reason to switch.
- **You want navigation properties and lazy loading.** If you understand the performance implications and your use case benefits from transparent related-data resolution, EF provides this out of the box.

## When Zonkey Fits Better

- **You want full control over every database operation.** Every query, every save, every delete is an explicit method call that you write and can reason about.
- **You need to audit or predict exactly what SQL executes.** Zonkey generates straightforward SQL with no surprises. You can attach a `BeforeExecuteCommand` handler to log every command.
- **You are building high-performance services where implicit behavior causes problems.** No change tracker overhead, no lazy loading surprises, no large context graphs accumulating in memory.
- **You prefer explicit data loading patterns.** Loading related data with separate queries gives you precise control over what data enters memory and when.
- **You work with stored procedures or complex SQL alongside mapped objects.** `DataManager` handles raw SQL and stored procedures natively, and `DataClassAdapter` can fill objects from stored procedure results via `FillWithSP`.

---

[Back to documentation index](README.md) | [Project README](../README.md)
