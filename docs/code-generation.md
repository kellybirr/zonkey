# Code Generation

`zonkey-scaffold` generates Zonkey data classes and a `DatabaseWrapper` from a live database. It replaces the two legacy generators (`Zonkey.CodeGen`, a WinForms/SMO tool, and `NpgCodeGen`) with one cross-platform CLI.

The output is a **starting point**. It is meant to be read, renamed, and edited — not regenerated forever. If a class name collides with something in your project, or a column maps to a type you disagree with, change the file.

## Install

```bash
dotnet tool install -g zonkey.scaffold
```

Or run it from the repo without installing:

```bash
dotnet run --project tools/Zonkey.Scaffold -- --help
```

## Usage

```bash
zonkey-scaffold --provider pgsql \
                --connection "Host=localhost;Database=zoo;Username=app;Password=…" \
                --namespace Zoo.Data \
                --out ./Data
```

Providers: `sqlite`, `pgsql` (also `postgres`, `postgresql`), `mysql` (also `mariadb`), `mssql` (also `sqlserver`).

### Common options

| Option | Meaning |
| --- | --- |
| `-p`, `--provider` | Database provider |
| `-c`, `--connection` | ADO.NET connection string |
| `-n`, `--namespace` | Namespace for generated classes |
| `-o`, `--out` | Output directory (default: current) |
| `--schema` | Schema to read; omit for all non-system schemas |
| `--wrapper-class` | Wrapper class name (default: `AppDatabase`) |
| `--Language` | `CSharp` (default) or `VB` |
| `--dry-run` | Report what would be written, write nothing |

Settings are bound with `IConfiguration`, so **any** member of `ScaffoldOptions` is settable as `--Section:Key value`:

```bash
zonkey-scaffold … --IgnoreTables "__*;aspnet_*" \
                  --Views true \
                  --Naming:Singularize false \
                  --Emit:FieldKeyword false \
                  --Emit:PrivateFieldsAtTop true
```

The same keys load from a `zonkey.scaffold.json` file in the working directory and from `ZONKEY_SCAFFOLD_*` environment variables. Precedence is file, then environment, then command line.

Never put a connection string in the JSON file if the file is committed — pass it on the command line or via `ZONKEY_SCAFFOLD_ConnectionString`.

## What it generates

One file per table, plus one wrapper:

```csharp
[DataItem("animals")]
public partial class Animal : DataClass
{
    public Animal(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use Animal(bool addingNew) in code.", true)]
    public Animal() : this(false) { }

    [DataField("animal_id", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
    public int AnimalId { get => field; set => SetFieldValue(ref field, value); }

    [DataField("name", DbType.String, false, Length = 80)]
    public string Name { get => field; set => SetFieldValue(ref field, value); } = null!;
}
```

```csharp
public partial class AppDatabase : DatabaseWrapper
{
    public AppDatabase() : base("Default") { }
    public AppDatabase(DbConnection connection) : base(connection) { }

    public DataClassAdapter<Animal> Animals => Adapter<Animal>();
}
```

Classes are `partial`, so put your own members in a separate file rather than editing the generated one if you intend to regenerate.

The parameterless constructor is marked `[Obsolete(…, true)]` deliberately: `DataClassAdapter` needs it to materialize rows, but your code should always use `new Animal(true)` for a new row. See [Data Classes](data-classes.md).

Tables without a primary key, and views, are emitted read-only (`{ get; set; }` auto-properties, no change tracking).

## Relations

`--Emit:Relations true` adds in-memory graph members derived from foreign keys — a `Species` reference on the child, an `Animals` list on the parent:

```csharp
// Related data. These have no [DataField], so the adapter never reads or writes
// them — nothing is loaded until you fill them yourself.
// public.animals.species_id -> public.species
public Species? Species { get; set; }
```

They carry **no `[DataField]`**, so the adapter never selects, inserts, or updates them. Nothing loads them implicitly — Zonkey has no navigation properties and no lazy loading, and the scaffolder does not invent any.

What it does emit is an explicit loader per relation, in a `{Entity}Extensions` class keyed by the entity being *queried*:

```csharp
var orders = new List<Order>();
await db.Orders.Fill(orders, o => o.PlacedOn >= since);

await db.OrderDetails.FillOrderDetailsFor(orders);   // one query, not one per order
await db.Customers.FillCustomerFor(orders);
await db.Addresses.FillShipToFor(orders);            // two FKs into one table stay distinct
await db.Addresses.FillBillToFor(orders);
```

Each method is overloaded for a single owner and for `IEnumerable<T>`, and **you supply the adapter** — so the query runs on your transaction, your timeout, and the wrapper's cached adapter. Nothing constructs a connection or an adapter behind your back.

The batched form is one query for any number of owners: distinct keys into a `Contains`, then an in-memory `ToLookup`/dictionary join. Keys are de-duplicated, and owners with a null foreign key are skipped. Scaling is the translator's job — see [Translation policy](querying.md#translation-policy). On PostgreSQL the whole key list binds as one array parameter at any size; on the other dialects integer and `Guid` keys inline as literals past 64 values, while string keys stay parameterized and throw past the dialect's cap with a hint to batch with `Chunk`.

Composite foreign keys can't be expressed as one `IN`, so they get the members but no loader, with a warning.

The parent reference is named from the FK column with its `Id` suffix removed (`species_id` → `Species`), which keeps two keys into the same table distinguishable (`ShipToAddress`, `BillToAddress`). A member whose name collides with a mapped column is skipped with a warning — the column wins.

Foreign keys pointing at tables outside the current run are ignored.

## VB.NET

`--Language VB` emits `.vb` files with the same structure. VB has no `field` keyword, so properties always declare an explicit backing field (`Emit:FieldKeyword` is not reachable), and it has no nullable reference types, so `Emit:NullableRefs` affects value types only.

**One thing that will catch you:** VB prepends the project's `RootNamespace` to every declared namespace. Scaffolding with `--namespace Zoo.Data` into a project whose root namespace is `MyApp` gives you `MyApp.Zoo.Data`. Either clear `<RootNamespace></RootNamespace>` in the vbproj, or pass the namespace you want appended. C# has no such behavior.

## For coding agents

The tool ships a loadable skill covering the workflow and the mistakes that bite. Install it into your project:

```bash
zonkey-scaffold skill --install          # writes .claude/skills/zonkey-scaffold/SKILL.md
zonkey-scaffold skill --install --out ./somewhere-else
```

Re-running it updates the file in place, so you can refresh it after a tool upgrade. The source is [`.claude/skills/zonkey-scaffold/SKILL.md`](../.claude/skills/zonkey-scaffold/SKILL.md), which also means it loads automatically for agents working in this repo.

## Notes

- Type mapping is per provider. Unrecognized column types map to `string` / `DbType.String` and print a warning naming the column — change the generated `DataField` if that is wrong.
- PostgreSQL folds unquoted identifiers to lower case. A mixed-case table or column created with quotes will not resolve unless you quote identifiers (`AdapterProperty.UseQuotedIdentifiers`).
- Files are written with a `.g.cs` suffix by default (`--Output:GeneratedSuffix false` to drop it).
