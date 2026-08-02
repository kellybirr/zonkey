---
name: zonkey-scaffold
description: Use when generating a Zonkey data layer from an existing database — the user has a connection string and wants data classes, or asks to scaffold/generate/reverse-engineer tables into C#. Covers running zonkey-scaffold and what to do with its output.
---

# Scaffolding a Zonkey data layer

`zonkey-scaffold` reads a live database and writes Zonkey data classes plus a `DatabaseWrapper`.

**Its output is a draft.** It compiles in the common case, but it is meant to be reviewed and edited. Do not build machinery to make it perfect — read it, fix what's wrong, move on.

## Run it

```bash
dotnet tool install -g zonkey.scaffold     # once
zonkey-scaffold skill --install            # optional: drops this skill into the current project
zonkey-scaffold --provider pgsql \
                --connection "Host=localhost;Database=zoo;Username=app;Password=…" \
                --namespace Zoo.Data \
                --out ./Data
```

Providers: `sqlite`, `pgsql`, `mysql`, `mssql` (aliases: `postgres`/`postgresql`, `mariadb`, `sqlserver`).

Add `--dry-run` first if you want to see the file list before writing.

Settings bind through `IConfiguration`, so any option is `--Section:Key value`:

```bash
--IgnoreTables "__*;aspnet_*"    # trailing * allowed, ;-separated
--schema public                  # omit for all non-system schemas
--Views true                     # include views (emitted read-only)
--Naming:Singularize false       # keep table names plural
--Emit:FieldKeyword false        # explicit backing fields instead of `field`
--Emit:NullableRefs false
--wrapper-class ZooDb
```

They also load from `zonkey.scaffold.json` and `ZONKEY_SCAFFOLD_*`. **Never write a connection string into the JSON file** — pass it on the command line or via the environment.

## After running

1. **Read the generated files.** Especially the `DbType` and CLR type on anything unusual.
2. **Act on any warning it printed.** Unrecognized column types map to `string`/`DbType.String` — that is a guess, and it names the column so you can correct the `DataField`.
3. **Rename freely.** Two tables producing the same class name write two files and the second wins; the tool does not stop you. Rename one and re-run with `--IgnoreTables` for the other, or just edit.
4. **Check `.g.cs` files into source control or not, as the project prefers** — but put your own members in a *separate* partial class file, because regenerating overwrites.

## What the output looks like

```csharp
[DataItem("animals")]
public partial class Animal : DataClass
{
    public Animal(bool addingNew) : base(addingNew) { }

    [Obsolete("Required by the DataClassAdapter materializer; use Animal(bool addingNew) in code.", true)]
    public Animal() : this(false) { }

    [DataField("animal_id", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
    public int AnimalId { get => field; set => SetFieldValue(ref field, value); }
}
```

The `[Obsolete(…, true)]` parameterless constructor is **required and intentional**. `DataClassAdapter` needs it to materialize rows; your code must never call it. `new Animal()` yields a `Detached` object and saving one throws `InvalidOperationException` — the attribute turns that into a compile error. Never remove it, and never write `new Animal()`; use `new Animal(addingNew: true)`.

## Things that will bite you

- **PostgreSQL folds unquoted identifiers to lower case.** A table created as `"OrderLines"` will not resolve unless quoting is on (`AdapterProperty.UseQuotedIdentifiers`). Prefer lowercase schema objects.
- **PostgreSQL timestamps:** plain `timestamp` → `DbType.DateTime2`; `timestamptz` → `DbType.DateTime` with `DateTimeKind.Utc`. The tool gets this right — don't "fix" it, and never enable Npgsql's legacy timestamp behavior.
- **Tables with no primary key, and views, are emitted read-only** (auto-properties, no change tracking). That is deliberate: Zonkey cannot build an UPDATE without a key.
- **Async methods are suffix-less** (`Fill`, `Save`, `GetOne`). There are no `…Async` variants.

## Then use it

```csharp
await using var db = new AppDatabase();
var animals = await db.Animals.Fill(a => a.SpeciesId == id);
```

See [docs/code-generation.md](../../../docs/code-generation.md) for the full option list and [docs/data-classes.md](../../../docs/data-classes.md) for the data-class contract.
