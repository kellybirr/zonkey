# Zonkey.Mocks

Mock implementations of core ADO.NET classes for unit testing Zonkey-based data access code without a real database.

## Installation

```shell
dotnet add package Zonkey.Mocks
```

## What's Inside

- **MockDbConnection** -- mock connection with configurable command setup
- **MockDbCommand** -- mock command with delegate-based behavior (`DoExecuteReader`, `DoExecuteScalar`, `DoExecuteNonQuery`)
- **MockDbDataReader** -- mock reader backed by DataTable, collections, or dictionaries
- **MockDbParameter / MockDbParameterCollection** -- mock parameter objects
- **MockDbTransaction** -- mock transaction with state tracking (`Uncomitted`, `Comitted`, `RolledBack`)

## Quick Example

```csharp
using Zonkey.Mocks;

var connection = new MockDbConnection();
connection.SetupCommandFunc = cmd =>
{
    cmd.DoExecuteReader = c =>
    {
        var dt = new DataTable();
        dt.Columns.Add("id", typeof(int));
        dt.Columns.Add("name", typeof(string));
        dt.Columns.Add("price", typeof(decimal));
        dt.Rows.Add(1, "Classic Tee", 24.99m);
        return dt;
    };
};
connection.Open();

var adapter = new DataClassAdapter<Product>(connection);
var product = await adapter.GetOne(p => p.Id == 1);
// product.Name == "Classic Tee"
```

## Use Cases

- Test data mapping logic (column-to-property)
- Verify SQL generation and parameter binding
- Test save behavior (insert vs update detection)
- Test transaction state management
- Test error handling paths

## Target Frameworks

- .NET 8.0
- .NET 10.0
- .NET Framework 4.8

## Documentation

See the [testing guide](../../docs/testing.md) for detailed patterns and the [full documentation](../../docs/).
