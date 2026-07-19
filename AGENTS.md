# Zonkey — Guide for AI Coding Agents

Zonkey is a deterministic, explicit, async-first .NET ORM (packages: `Zonkey.Data`, `Zonkey.Data.MsSql`, `Zonkey.Text`, `Zonkey.Mocks`). This file orients coding agents working on this repository or on projects that consume it. The full source is public and the NuGet assemblies carry Source Link with embedded PDBs, so symbols always resolve to the exact commit on GitHub.

## Build & Test

```shell
dotnet build Zonkey.sln                                 # all projects, all target frameworks
dotnet test test/Zonkey.Tests/Zonkey.Tests.csproj       # xUnit v3 suite (net10.0 + net48)
docker compose up -d --wait                             # MSSQL (host port 1434) + PostgreSQL (host port 5433) for integration tests
```

SQLite integration tests always run; MSSQL/PostgreSQL tests skip gracefully when the containers are down. Connection overrides: `ZONKEY_TEST_MSSQL`, `ZONKEY_TEST_PGSQL`. Targets are net6.0/net8.0/net10.0/net48; `Directory.Build.props` makes net48 build on Linux.

## Documentation Map

Start with these two for the mental model, then use the rest as reference:

- [docs/architecture.md](docs/architecture.md) — how a query and a save actually flow through the layers, and why
- [docs/overview.md](docs/overview.md) — the design philosophy (determinism, no hidden behavior)

Reference: [data classes](docs/data-classes.md) · [adapter/CRUD](docs/data-class-adapter.md) · [querying](docs/querying.md) · [relationships](docs/modeling-relationships.md) · [providers & dialects](docs/database-providers.md) · [transactions](docs/transactions.md) · [EF migration](docs/migrating-from-ef.md) · [full index](docs/README.md)

## Rules That Prevent the Most Common Agent Mistakes

1. **No method calls inside query lambdas.** `adapter.GetOne(a => a.Id == Guid.Parse(x))` throws `NotSupportedException` — hoist the value into a local variable first. The lambda parser is a WHERE-clause translator, not a C# evaluator.
2. **Async methods are deliberately suffix-less** (`Fill`, `Save`, `GetOne`). Do not rename them or search for `FillAsync` — the suffix-less names are the async API; there are no sync variants.
3. **Data-class pattern:** properties call `SetFieldValue(ref field, value)`; classes need `public X() : base(false)` (materializer) and `public X(bool addingNew) : base(addingNew)` (new records use `new X(addingNew: true)`).
4. **PostgreSQL case folding:** Zonkey emits unquoted identifiers by default; use lowercase schema names on PostgreSQL, or enable quoting — see [identifier quoting](docs/database-providers.md#identifier-quoting--case-sensitivity).
5. **`Save` returning `false` means skipped (Unchanged), not failed.** Failures throw.
6. **`SqlFilter` and string filters take database column names; lambdas take C# property names.**
7. Attribute `UseQuotedIdentifier` cannot be set in attribute syntax (`bool?`, CS0655) — runtime `DataMap` mutation or adapter `SetProperty` only.

## Repository Layout

- `src/Zonkey.Data` — core ORM (adapters, dialects, command builders, expression parser)
- `src/Zonkey.Data.MsSql`, `src/Zonkey.Text`, `src/Zonkey.Mocks` — extensions, text-file mapping, test mocks
- `test/Zonkey.Tests` — the test suite (unit + SQLite/MSSQL/PostgreSQL integration)
- `docs/` — full documentation; `docs/superpowers/` and `docs/todo-*.md` are internal planning records
