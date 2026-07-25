# Zonkey.Data.MsSql

SQL Server-specific extensions for Zonkey.Data. Provides Microsoft.Data.SqlClient integration and XML query support.

## Installation

```shell
dotnet add package Zonkey.Data.MsSql
```

Requires: Zonkey.Data, Microsoft.Data.SqlClient

## Setup

Call `Initialize()` at application startup:

```csharp
using Zonkey;

MsSqlExtension.Initialize();
```

This registers the Microsoft.Data.SqlClient connection factory and configures proper SqlDbType.Time parameter handling.

## Features

### Provider Registration

Registers `Microsoft.Data.SqlClient.SqlConnection` with Zonkey's dialect system so the SQL Server dialect is automatically selected.

### SqlXmlAdapter

Work with SQL Server's FOR XML queries:

```csharp
using Zonkey.SqlServer;

var xmlAdapter = new SqlXmlAdapter(sqlConnection);
var doc = await xmlAdapter.GetXmlDocument("root",
    "SELECT * FROM products FOR XML PATH('product')", false);
```

Methods:

- `GetXmlDocument()` -- returns XmlDocument with wrapped root element
- `FillXmlNode()` -- populates an existing XML node
- `GetXmlString()` -- returns raw XML string

## Target Frameworks

- .NET 8.0
- .NET 10.0
- .NET Framework 4.8

## Documentation

See the [database providers guide](../../docs/database-providers.md) for dialect details and the [full documentation](../../docs/).
