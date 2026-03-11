# Zonkey.Text

A companion library for mapping CSV and fixed-width text files to C# objects using attribute-based configuration. Follows the same philosophy as Zonkey.Data but for flat files instead of databases.

## Installation

```shell
dotnet add package Zonkey.Text
```

## What's Inside

- **TextClassReader&lt;T&gt;** -- reads text files into strongly-typed objects
- **TextClassWriter&lt;T&gt;** -- writes objects to text files
- **CsvReader** -- low-level CSV parser implementing IDataReader
- **DynamicCsvReader** -- reads CSV into dynamic objects (no class definition needed)
- **TextRecordAttribute** -- class-level config (delimiter, record type, text qualifier)
- **TextFieldAttribute** -- property-level config (position, length, format)

## Quick Example

```csharp
using Zonkey.Text;

[TextRecord(TextRecordType.Delimited, Delimiter = ',', TextQualifier = '"')]
public class ProductImport
{
    [TextField(Position = 0)]
    public string Name { get; set; } = "";

    [TextField(Position = 1)]
    public decimal Price { get; set; }

    [TextField(Position = 2)]
    public string Category { get; set; } = "";
}
```

```csharp
using var reader = new TextClassReader<ProductImport>("products.csv");
var products = new List<ProductImport>();
reader.Fill(products);
```

## Supported Formats

- **Delimited** (CSV, TSV, pipe-delimited, etc.) -- configurable delimiter and text qualifier
- **Fixed-width** -- fields defined by position and length

## Target Frameworks

- .NET Standard 2.0
- .NET Standard 2.1
- .NET Framework 4.8

## Documentation

See the [text file mapping guide](../../docs/text-files.md) for detailed usage and the [full documentation](../../docs/).
