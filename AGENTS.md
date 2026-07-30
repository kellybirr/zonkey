# Zonkey — Guide for AI Coding Agents

Zonkey is a deterministic, explicit, async-first .NET ORM (packages: `Zonkey.Data`, `Zonkey.Data.MsSql`, `Zonkey.Text`, `Zonkey.Mocks`). This file orients coding agents working on this repository or on projects that consume it. The full source is public and the NuGet assemblies carry Source Link with embedded PDBs, so symbols always resolve to the exact commit on GitHub.

## Build & Test

```shell
dotnet build Zonkey.sln                                 # all projects, all target frameworks
dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj       # xUnit v3 suite (net10.0 + net48)
docker compose up -d --wait                             # MSSQL (host port 1434) + PostgreSQL (host port 5433) + MySQL (host port 3308) for integration tests
```

SQLite integration tests always run; MSSQL/PostgreSQL/MySQL tests skip gracefully when the containers are down. Connection overrides: `ZONKEY_TEST_MSSQL`, `ZONKEY_TEST_PGSQL`, `ZONKEY_TEST_MYSQL`. Targets are net8.0/net10.0/net48 (Zonkey.Text: netstandard2.0/net48); `Directory.Build.props` makes net48 build on Linux.

## Documentation Map

Start with these two for the mental model, then use the rest as reference:

- [docs/architecture.md](docs/architecture.md) — how a query and a save actually flow through the layers, and why
- [docs/overview.md](docs/overview.md) — the design philosophy (determinism, no hidden behavior)

Reference: [data classes](docs/data-classes.md) · [adapter/CRUD](docs/data-class-adapter.md) · [querying](docs/querying.md) · [relationships](docs/modeling-relationships.md) · [providers & dialects](docs/database-providers.md) · [transactions](docs/transactions.md) · [EF migration](docs/migrating-from-ef.md) · [full index](docs/README.md)

## Rules That Prevent the Most Common Agent Mistakes

1. **Method calls in query lambdas are fine as long as they don't operate on the mapped property.** `adapter.GetOne(a => a.Id == Guid.Parse(x))` works — a partial evaluator folds any subexpression that doesn't reference the lambda parameter (method calls, indexers, statics) to a value client-side before translation, no hoisting needed. Only method calls made *on* the parameter (e.g. `a.Name.PadLeft(5)`) are limited to a fixed set of registered translations and throw `SqlExpressionException` (derives from `NotSupportedException`) if untranslatable. See [docs/querying.md](docs/querying.md#how-expressions-are-translated).
2. **Async methods are deliberately suffix-less** (`Fill`, `Save`, `GetOne`). Do not rename them or search for `FillAsync` — the suffix-less names are the async API; there are no sync variants.
3. **Data-class pattern:** properties call `SetFieldValue(ref field, value)`. Classes need `public X(bool addingNew) : base(addingNew)` — the one your code calls, as `new X(addingNew: true)` — plus a public parameterless constructor **for the materializer only**, which must be marked `[Obsolete("...", true)]`:
   ```csharp
   public X(bool addingNew) : base(addingNew) { }

   [Obsolete("Required by the DataClassAdapter materializer; use X(bool addingNew) in code.", true)]
   public X() : this(false) { }
   ```
   Never write `new X()`. It yields a `Detached` object, and saving a `Detached` object throws `InvalidOperationException`. The attribute makes that a compile error instead. It costs nothing: `Obsolete` is compile-time metadata, so the IL-emitting materializer ignores it, and it does not interfere with the `where Tdc : class, new()` constraint on `Adapter<Tdc>()`.
4. **PostgreSQL case folding:** Zonkey emits unquoted identifiers by default; use lowercase schema names on PostgreSQL, or enable quoting — see [identifier quoting](docs/database-providers.md#identifier-quoting--case-sensitivity).
5. **`Save` returning `false` means skipped (Unchanged), not failed.** Failures throw.
6. **`SqlFilter` and string filters take database column names; lambdas take C# property names.**
7. Attribute `UseQuotedIdentifier` cannot be set in attribute syntax (`bool?`, CS0655) — runtime `DataMap` mutation or adapter `SetProperty` only.
8. **PostgreSQL timestamps:** a plain `timestamp` column must be declared `DbType.DateTime2`; a `timestamptz` column must be `DbType.DateTime` with `DateTimeKind = DateTimeKind.Utc` (Npgsql maps `DbType.DateTime` to `timestamptz`). NEVER enable Npgsql's legacy timestamp behavior — fix the declaration instead. See [docs/postgresql.md](docs/postgresql.md).

## Repository Layout

- `src/Zonkey.Data` — core ORM (adapters, dialects, command builders, expression parser)
- `src/Zonkey.Data.MsSql`, `src/Zonkey.Text`, `src/Zonkey.Mocks` — extensions, text-file mapping, test mocks
- `test/Zonkey.Tests` — the test suite (unit + SQLite/MSSQL/PostgreSQL integration)
- `docs/` — full documentation; `docs/superpowers/` and `docs/todo-*.md` are internal planning records
