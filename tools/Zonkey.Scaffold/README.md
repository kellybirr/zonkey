# zonkey-scaffold

Generates [Zonkey](https://github.com/kellybirr/zonkey) data classes and a `DatabaseWrapper` from a live database. SQLite, PostgreSQL, MySQL/MariaDB, and SQL Server.

```shell
dotnet tool install -g zonkey.scaffold

zonkey-scaffold --provider pgsql \
                --connection "Host=localhost;Database=zoo;Username=app;Password=..." \
                --namespace Zoo.Data \
                --out ./Data
```

The output is a **starting point** — review it, rename what you like, and edit it in place. It is not meant to be regenerated forever.

## Options

| Option | Meaning |
| --- | --- |
| `-p`, `--provider` | `sqlite`, `pgsql`, `mysql`, `mssql` |
| `-c`, `--connection` | ADO.NET connection string |
| `-n`, `--namespace` | Namespace for generated classes |
| `-o`, `--out` | Output directory (default: current) |
| `--schema` | Schema to read; omit for all non-system schemas |
| `--wrapper-class` | Wrapper class name (default: `AppDatabase`) |
| `--Language` | `CSharp` (default) or `VB` |
| `--Emit:Relations` | Emit graph members from foreign keys, plus batched `Fill…For` loaders |
| `--dry-run` | Report what would be written, write nothing |

Settings bind through `IConfiguration`, so anything else is `--Section:Key value`:

```shell
zonkey-scaffold ... --IgnoreTables "__*;aspnet_*" --Views true --Emit:FieldKeyword false
```

The same keys load from `zonkey.scaffold.json` and `ZONKEY_SCAFFOLD_*` environment variables (file, then environment, then command line). Don't put a connection string in a committed JSON file.

## For coding agents

```shell
zonkey-scaffold skill --install
```

Writes a skill to `.claude/skills/zonkey-scaffold/SKILL.md` covering the workflow and the provider-specific traps.

## Documentation

[Code generation](https://github.com/kellybirr/zonkey/blob/master/docs/code-generation.md) · [Data classes](https://github.com/kellybirr/zonkey/blob/master/docs/data-classes.md) · [Full docs](https://github.com/kellybirr/zonkey)
