# Modeling Relationships

Zonkey has no navigation properties, no lazy loading, and no cascades -- deliberately. Related data is loaded with explicit queries and stitched in memory, and saved in an order you control. This doc shows the canonical patterns for doing that well.

**The trade, stated plainly:** in exchange for writing the stitching code yourself, every query is one you can see, count, and index for. There is no N+1 surprise because there is no mechanism that could cause one -- the only way to query in a loop is to write a loop, and this doc shows how to avoid wanting to.

The examples use a small order-taking domain:

```csharp
[DataItem("customers")]
public class Customer : DataClass
{
    public Customer(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use Customer(bool addingNew) in code.", true)]
    public Customer() : this(false) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public int Id { get => field; set => SetFieldValue(ref field, value); }

    [DataField("name", DbType.String, false, Length = 100)]
    public string Name { get => field; set => SetFieldValue(ref field, value); } = "";
}

[DataItem("orders")]
public class Order : DataClass
{
    public Order(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use Order(bool addingNew) in code.", true)]
    public Order() : this(false) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public int Id { get => field; set => SetFieldValue(ref field, value); }

    [DataField("customer_id", DbType.Int32, false)]
    public int CustomerId { get => field; set => SetFieldValue(ref field, value); }

    [DataField("order_date", DbType.DateTime, false, DateTimeKind = DateTimeKind.Utc)]
    public DateTime OrderDate { get => field; set => SetFieldValue(ref field, value); }

    // Unmapped member: no [DataField], so the adapter never reads or writes it.
    // This is where stitched-in children live.
    public List<OrderLine> Lines { get; } = [];
}

[DataItem("order_lines")]
public class OrderLine : DataClass
{
    public OrderLine(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use OrderLine(bool addingNew) in code.", true)]
    public OrderLine() : this(false) { }

    [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
    public int Id { get => field; set => SetFieldValue(ref field, value); }

    [DataField("order_id", DbType.Int32, false)]
    public int OrderId { get => field; set => SetFieldValue(ref field, value); }

    [DataField("product_id", DbType.Int32, false)]
    public int ProductId { get => field; set => SetFieldValue(ref field, value); }

    [DataField("quantity", DbType.Int32, false)]
    public int Quantity { get => field; set => SetFieldValue(ref field, value); }

    [DataField("unit_price", DbType.Decimal, false)]
    public decimal UnitPrice { get => field; set => SetFieldValue(ref field, value); }
}
```

(The examples use the C# 14 `field` keyword for brevity -- on older compilers, use the explicit backing-field pattern shown in [Data Classes](data-classes.md#using-the-field-keyword-net-10).)

Two things to notice:

- **Foreign keys are ordinary mapped fields** (`CustomerId`, `OrderId`). There is no special relationship attribute; the FK column is the relationship.
- **Members without `[DataField]` are invisible to the adapter** -- `Order.Lines` is never selected, inserted, or updated. It exists purely for your in-memory object graph. (Caveat: this relies on explicit field attributes. If you use `ImplicitFieldDefinition = true`, *every* property gets mapped, so keep navigation members off such classes.)

---

## Loading One Parent with Children

Two queries, explicitly:

```csharp
var order = await db.GetOne<Order>(o => o.Id == orderId);
await db.Adapter<OrderLine>().Fill(order.Lines, l => l.OrderId == order.Id);
```

That is the entire pattern. Two round trips, both indexable (`order_lines.order_id` should have an index -- Zonkey expects you to know that, per its philosophy).

---

## Loading Many Parents with Children (avoiding N+1)

The wrong way is the loop you would never write on purpose:

```csharp
// DON'T: one query per order
foreach (var order in orders)
    await lineAdapter.Fill(order.Lines, l => l.OrderId == order.Id);
```

The right way is two queries total -- fetch all children for all parents with `SqlInInt`, then stitch with a lookup:

```csharp
using Zonkey.Extensions;

// 1. Load the parents
var orders = new List<Order>();
await db.Adapter<Order>().Fill(orders, o => o.OrderDate >= since);

// 2. Load ALL their children in one query
var orderIds = orders.Select(o => o.Id).ToArray();
var allLines = new List<OrderLine>();
if (orderIds.Length > 0)
    await db.Adapter<OrderLine>().Fill(allLines, l => l.OrderId.SqlInInt(orderIds));

// 3. Stitch in memory
var byOrder = allLines.ToLookup(l => l.OrderId);
foreach (var order in orders)
    order.Lines.AddRange(byOrder[order.Id]);
```

`SqlInInt` emits the ids as inline literals, so this scales past parameter-count limits; for very large id sets, batch with `SplitList` (see [Querying](querying.md#large-lists-with-splitlist)). Remember the parser rule: the id array must be a *variable*, not an inline expression.

---

## Many-to-One and Reference Data

For lookups (the `Product` an `OrderLine` points at), load the reference table once and index it -- do not join per row:

```csharp
var products = await db.Adapter<Product>().GetList("1=1");
var productById = products.ToDictionary(p => p.Id);

foreach (var line in order.Lines)
    Console.WriteLine($"{productById[line.ProductId].Name} x {line.Quantity}");
```

For reference data that rarely changes, cache the dictionary at application scope; Zonkey deliberately has no identity map, so caching policy is yours.

---

## Denormalized Read Models

When a screen needs joined data, do not simulate a join in memory -- create a database view and map a read-only class to it:

```sql
CREATE VIEW order_line_details AS
SELECT ol.id, ol.order_id, ol.quantity, ol.unit_price, p.name AS product_name
FROM order_lines ol JOIN products p ON p.id = ol.product_id;
```

```csharp
[DataItem("order_line_details")]
public class OrderLineDetail   // no DataClass base: read-only by construction
{
    [DataField("id", DbType.Int32, IsKeyField = true)]
    public int Id { get; set; }

    [DataField("order_id", DbType.Int32)]
    public int OrderId { get; set; }

    [DataField("quantity", DbType.Int32)]
    public int Quantity { get; set; }

    [DataField("unit_price", DbType.Decimal)]
    public decimal UnitPrice { get; set; }

    [DataField("product_name", DbType.String)]
    public string ProductName { get; set; } = "";
}
```

If a class should *read* from a view but *write* to the underlying table, keep it savable and set `[DataItem("view_name", SaveToTable = "table_name")]` -- SELECTs hit the view, INSERT/UPDATE hit the table.

---

## Saving a Graph

There is no cascade, so you save in dependency order -- parent first (to obtain its identity), then children with the FK set -- inside one transaction:

```csharp
await db.WithTransaction(async trx =>
{
    var orderAdapter = db.Adapter<Order>(trx);
    var lineAdapter = db.Adapter<OrderLine>(trx);

    await orderAdapter.Save(order);          // select-back populates order.Id

    foreach (var line in order.Lines)
    {
        line.OrderId = order.Id;             // wire the FK explicitly
        await lineAdapter.Save(line);
    }
});
```

`WithTransaction` commits on success and rolls back on exception, so a failed line save unwinds the order insert too. For deletes, invert the order: children first, then the parent -- the database's FK constraints are your safety net, and Zonkey will surface their violations rather than silently reordering anything.

For collections where items were removed in the UI, `SaveCollection` handles deletes-first automatically when the collection implements `ITrackDeletedItems<T>` (as `BindableCollection` does): deleted items are processed before added and modified ones.

---

## Many-to-Many

A junction table is just another mapped class:

```csharp
[DataItem("order_tags")]
public class OrderTag : DataClass
{
    public OrderTag(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use OrderTag(bool addingNew) in code.", true)]
    public OrderTag() : this(false) { }

    [DataField("order_id", DbType.Int32, IsKeyField = true)]
    public int OrderId { get => field; set => SetFieldValue(ref field, value); }

    [DataField("tag_id", DbType.Int32, IsKeyField = true)]
    public int TagId { get => field; set => SetFieldValue(ref field, value); }
}
```

Composite key, no auto-increment. Linking is inserting a row; unlinking is `DeleteItem` or a filtered `Delete`; "all tags for these orders" is a `SqlInInt` fill on the junction plus a dictionary lookup into the tags -- the same stitching pattern as above.

---

## Summary of the Rules

1. Foreign keys are plain mapped fields; unmapped members hold the in-memory graph.
2. One parent: two queries. Many parents: two queries -- parents, then `SqlInInt` children, then `ToLookup` stitching. Never a query inside a loop.
3. Reference data: load once, index in a dictionary, cache at your own policy.
4. Joins belong in the database: map read-only classes to views; use `SaveToTable` for write-through.
5. Save parent-first / delete children-first, inside `WithTransaction`.

## See Also

- [Architecture](architecture.md) -- what happens beneath each of these calls
- [Querying](querying.md) -- `SqlIn` family, parser limitations, pagination
- [Migrating from Entity Framework](migrating-from-ef.md) -- where these patterns replace navigation properties
