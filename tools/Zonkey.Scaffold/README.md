# zonkey.scaffold

Scaffolds [Zonkey](https://github.com/kellybirr/zonkey) data classes and a `DatabaseWrapper`
from a live database schema.

```shell
dotnet tool install -g zonkey.scaffold
export ZONKEY_SCAFFOLD_ConnectionString="Data Source=./app.db"
zonkey-scaffold inspect --provider sqlite --json
zonkey-scaffold generate --provider sqlite --namespace MyApp.Data --out ./Data
```

Full documentation: [docs/scaffolding.md](https://github.com/kellybirr/zonkey/blob/master/docs/scaffolding.md)
