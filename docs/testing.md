# Testing with Zonkey.Mocks

The `Zonkey.Mocks` package provides mock implementations of core ADO.NET classes, enabling unit testing of data access code without a real database connection.

---

## Overview

Install:

```shell
dotnet add package Zonkey.Mocks
```

Zonkey.Mocks provides:

- `MockDbConnection` -- mock `DbConnection` with configurable command creation
- `MockDbCommand` -- mock `DbCommand` with delegate properties for each execution method
- `MockDbDataReader` -- mock reader backed by `DataTable`, collections, or dictionaries
- `MockDbParameter` / `MockDbParameterCollection` -- mock parameter objects
- `MockDbTransaction` -- mock transaction with state tracking

---

## Mock Connection

`MockDbConnection` extends `DbConnection`. Its `Open()` and `Close()` methods transition the `State` property between `ConnectionState.Open` and `ConnectionState.Closed`.

```csharp
using Zonkey.Mocks;

var connection = new MockDbConnection();
connection.Open();

// Configure what happens when commands are created
connection.SetupCommandFunc = cmd =>
{
    // Set up command behavior based on CommandText
    if (cmd.CommandText?.Contains("products") == true)
    {
        cmd.DoExecuteReader = c => CreateProductDataTable();
    }
};
```

Key members of `MockDbConnection`:

- `State` -- returns `ConnectionState.Open` or `ConnectionState.Closed` (transitions on `Open`/`Close`)
- `DataSource` -- returns `"Zonkey Mock DataSource"`
- `Database` -- returns `"Zonkey Mock Database"`
- `SetupCommandFunc` -- `Action<MockDbCommand>` called after each command is created; use this to configure the command's behavior based on its state
- `CreateCommandFunc` -- `Func<MockDbConnection, MockDbCommand>` to override command creation entirely (not commonly needed)

---

## Mock Command

`MockDbCommand` exposes delegate properties that control what happens when execution methods are called:

```csharp
var connection = new MockDbConnection();
connection.SetupCommandFunc = cmd =>
{
    // Mock a scalar query
    cmd.DoExecuteScalar = c => 42;

    // Mock a non-query
    cmd.DoExecuteNonQuery = c => 1; // 1 row affected

    // Mock a data reader -- return a DataTable or IEnumerable
    cmd.DoExecuteReader = c =>
    {
        var dt = new DataTable();
        dt.Columns.Add("id", typeof(int));
        dt.Columns.Add("name", typeof(string));
        dt.Columns.Add("price", typeof(decimal));
        dt.Rows.Add(1, "Classic Tee", 24.99m);
        dt.Rows.Add(2, "V-Neck", 29.99m);
        return dt;
    };
};
```

Delegate properties on `MockDbCommand`:

- `DoExecuteReader` -- `Func<MockDbCommand, object>`: return a `DataTable`, `IEnumerable`, or `IDictionary` to serve as the data source for a `MockDbDataReader`
- `DoExecuteNonQuery` -- `Func<MockDbCommand, int>`: return the number of rows affected
- `DoExecuteScalar` -- `Func<MockDbCommand, object>`: return the scalar value
- `DoCancel` -- `Action<MockDbCommand>`: called when `Cancel()` is invoked

The command also exposes `CommandText`, `CommandTimeout`, `CommandType`, and `Parameters` (a `MockDbParameterCollection`) for inspection in test assertions.

---

## Mock Data Reader

`MockDbDataReader` is created internally by `MockDbCommand` when `ExecuteReader` is called. The `DoExecuteReader` delegate returns the backing data source, which can be:

**A DataTable:**

```csharp
cmd.DoExecuteReader = c =>
{
    var dt = new DataTable();
    dt.Columns.Add("id", typeof(int));
    dt.Columns.Add("name", typeof(string));
    dt.Rows.Add(1, "Classic Tee");
    dt.Rows.Add(2, "V-Neck");
    return dt;
};
```

**A collection of objects (anonymous types or POCOs):**

```csharp
cmd.DoExecuteReader = c => new[]
{
    new { id = 1, name = "Classic Tee", price = 24.99m },
    new { id = 2, name = "V-Neck", price = 29.99m }
};
```

When using objects, the reader reflects over the public instance properties to determine column names and values.

**An IEnumerable of dictionaries:**

```csharp
cmd.DoExecuteReader = c => new[]
{
    new Dictionary<string, object>
    {
        ["id"] = 1,
        ["name"] = "Classic Tee",
        ["price"] = 24.99m
    }
};
```

---

## Mock Transaction

`MockDbConnection.BeginTransaction()` returns a `MockDbTransaction` that tracks its state:

```csharp
var connection = new MockDbConnection();
connection.Open();
var transaction = connection.BeginTransaction();

// transaction.State == MockTransactionState.Uncomitted
transaction.Commit();
// transaction.State == MockTransactionState.Comitted

// Or alternatively:
transaction.Rollback();
// transaction.State == MockTransactionState.RolledBack
```

Calling `Commit()` or `Rollback()` when the transaction is not in the `Uncomitted` state throws an `InvalidOperationException`. Only one active transaction is allowed per connection.

Note: the `MockTransactionState` enum values `Uncomitted` and `Comitted` use this exact spelling as defined in the library.

---

## Testing Data Access Code

Complete example testing a read operation with `DataClassAdapter<T>`:

```csharp
[TestMethod]
public async Task GetProduct_ReturnsProduct_WhenExists()
{
    // Arrange
    var connection = new MockDbConnection();
    connection.SetupCommandFunc = cmd =>
    {
        cmd.DoExecuteReader = c =>
        {
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("price", typeof(decimal));
            dt.Rows.Add(42, "Classic Tee", 24.99m);
            return dt;
        };
    };
    connection.Open();

    var adapter = new DataClassAdapter<Product>(connection);

    // Act
    var product = await adapter.GetOne(p => p.Id == 42);

    // Assert
    Assert.IsNotNull(product);
    Assert.AreEqual("Classic Tee", product.Name);
    Assert.AreEqual(24.99m, product.Price);
}
```

---

## Testing Save Operations

You can verify that save operations generate the expected SQL and parameters by inspecting the `MockDbCommand`:

```csharp
[TestMethod]
public async Task Save_NewProduct_Inserts()
{
    // Arrange
    string? executedSql = null;
    var connection = new MockDbConnection();
    connection.SetupCommandFunc = cmd =>
    {
        cmd.DoExecuteReader = c =>
        {
            executedSql = c.CommandText;
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Rows.Add(99); // Simulated identity value
            return dt;
        };
    };
    connection.Open();

    var adapter = new DataClassAdapter<Product>(connection);
    var product = new Product(addingNew: true) { Name = "Classic Tee", Price = 24.99m };

    // Act
    await adapter.Save(product);

    // Assert
    Assert.IsNotNull(executedSql);
    Assert.IsTrue(executedSql.Contains("INSERT"));
}
```

You can also inspect the parameters that were bound to the command:

```csharp
connection.SetupCommandFunc = cmd =>
{
    cmd.DoExecuteReader = c =>
    {
        // Inspect parameters
        foreach (MockDbParameter p in c.Parameters)
        {
            Console.WriteLine($"{p.ParameterName} = {p.Value}");
        }
        return resultTable;
    };
};
```

---

## What You Can and Cannot Test

**Can test with Zonkey.Mocks:**

- Data mapping (database columns to class properties)
- Save logic (insert vs update detection via `DataRowState`)
- Command text generation (verify the SQL output)
- Parameter binding (inspect command parameters)
- Transaction state management (commit, rollback, state transitions)
- Error handling paths (configure delegates to throw exceptions)

**Cannot test with mocks:**

- Actual SQL execution and result correctness
- Database-specific behavior (constraints, triggers, stored procedures)
- Connection pooling
- Real concurrency scenarios

For integration tests against a real database, use a test database with the actual ADO.NET provider.

---

## See Also

- [Getting Started](getting-started.md) -- initial setup and project configuration
- [DataClassAdapter](data-class-adapter.md) -- the primary adapter for typed CRUD operations
- [Back to README](../README.md)
