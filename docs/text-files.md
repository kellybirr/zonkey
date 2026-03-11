# Text File Mapping -- Zonkey.Text

Zonkey.Text is a companion library for reading and writing CSV and fixed-width text files using the same attribute-based mapping philosophy as Zonkey.Data. It is not for database access -- it is for flat file processing.

## Overview

Install:

```shell
dotnet add package Zonkey.Text
```

Key classes:

- `TextClassReader<T>` -- reads text files into typed objects
- `TextClassWriter<T>` -- writes typed objects to text files
- `CsvReader` -- low-level CSV parser (implements IDataReader)
- `DynamicCsvReader` -- reads CSV into dynamic (ExpandoObject) objects
- `TextRecordAttribute` -- class-level attribute (like DataItem)
- `TextFieldAttribute` -- property-level attribute (like DataField)

## Defining Text Classes

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

    [TextField(Position = 3, DateTimeStyle = DateTimeStyles.AssumeUniversal)]
    public DateTime CreatedDate { get; set; }
}
```

### TextRecordAttribute Properties

| Property | Description |
|---|---|
| `RecordType` | `Delimited` or `FixedLength` |
| `Delimiter` | Field separator character (default comma) |
| `TextQualifier` | Quote character for fields containing delimiters |
| `SequentialProperties` | Auto-assign positions based on property order |
| `NewLine` | Custom line terminator |

### TextFieldAttribute Properties

| Property | Description |
|---|---|
| `Position` | Zero-based field position (or auto-assigned if SequentialProperties is true) |
| `Length` | Field length (required for FixedLength records) |
| `NumberStyle` | `NumberStyles` for numeric parsing |
| `DateTimeStyle` | `DateTimeStyles` for date parsing |
| `OutputFormat` | Format string for writing |
| `BooleanTrue` | Custom true values for boolean fields (comma-separated, e.g. `"Yes,Y,1"`) |

## Reading CSV Files

`TextClassReader<T>` accepts a file path, stream, or `TextReader` in its constructor. After construction, call `Fill` to populate a collection or iterate directly using `foreach`.

```csharp
// From a file path
using var reader = new TextClassReader<ProductImport>("products.csv");
var products = new List<ProductImport>();
reader.Fill(products);
```

```csharp
// From a stream
using var stream = File.OpenRead("products.csv");
using var reader = new TextClassReader<ProductImport>(stream);
var products = new List<ProductImport>();
reader.Fill(products);
```

```csharp
// From a TextReader
using var textReader = new StreamReader("products.csv");
using var reader = new TextClassReader<ProductImport>(textReader);
var products = new List<ProductImport>();
reader.Fill(products);
```

```csharp
// With a filter predicate
using var reader = new TextClassReader<ProductImport>("products.csv");
var products = new List<ProductImport>();
reader.Fill(products, p => p.Price > 0);
```

### TextClassReader Properties

| Property | Description |
|---|---|
| `LineNumber` | Current line being processed |
| `ShortRecordBehaviour` | `Accept`, `Skip`, or `Exception` for short records |
| `LineFilter` | `Func<int, string, bool>` delegate to filter/skip lines (e.g., skip comments) |

### Enumeration

`TextClassReader<T>` implements `IEnumerable<T>`, so you can iterate directly:

```csharp
using var reader = new TextClassReader<ProductImport>("products.csv");

foreach (var product in reader)
{
    Console.WriteLine($"{product.Name}: {product.Price:C}");
}
```

## Writing Text Files

`TextClassWriter<T>` accepts a file path, stream, or `TextWriter` in its constructor. Call `Write` with a single object or a collection.

```csharp
// Write a collection to a file
using var writer = new TextClassWriter<ProductImport>("export.csv");
writer.Write(products);
```

```csharp
// Write to a stream
using var stream = File.Create("export.csv");
using var writer = new TextClassWriter<ProductImport>(stream);
writer.Write(products);
```

### Writer Options

| Property | Description |
|---|---|
| `TextQualifyAllFields` | Wrap all fields in the text qualifier character |
| `TextQualifyStrings` | Wrap string and char fields in the text qualifier (default `true`) |
| `MissingDelimitedFieldBehavior` | `Ignore`, `WriteAsNull`, or `WriteAsEmptyString` for gaps in field positions |

## Fixed-Width Files

```csharp
[TextRecord(TextRecordType.FixedLength)]
public class LegacyInventoryRecord
{
    [TextField(Position = 0, Length = 10)]
    public string ProductCode { get; set; } = "";

    [TextField(Position = 1, Length = 5)]
    public int Quantity { get; set; }

    [TextField(Position = 2, Length = 30)]
    public string WarehouseName { get; set; } = "";
}

using var reader = new TextClassReader<LegacyInventoryRecord>("inventory.dat");
var records = new List<LegacyInventoryRecord>();
reader.Fill(records);
```

For fixed-width records, each `TextFieldAttribute` must specify a `Length`. The `RecordLength` is automatically calculated from the field positions and lengths.

## Low-Level CSV Parsing

`CsvReader` implements `IDataReader` for low-level access without a mapped class:

```csharp
using var csvReader = new CsvReader(new StreamReader("products.csv"));
// Default delimiter is ',' and default text qualifier is '"'

while (csvReader.Read())
{
    var name = csvReader.GetString(0);
    var price = csvReader.GetDecimal(1);
}
```

### CsvReader Features

- `LineFilter` -- `Func<int, string, bool>` delegate for skipping lines (headers, comments)
- `ReadLine` -- `Func<TextReader, string>` delegate for preprocessing lines before parsing
- Configurable `Delimiter` and `TextQualifier` properties
- Handles escaped quotes within fields
- `GetString(index, maxLength)` overload to truncate long fields
- `LineNumber` property for tracking the current position
- `SourceLine` property for the raw text of the last line read

## Dynamic CSV Reading

Read CSV without defining a class. `DynamicCsvReader` reads the first row as column headers and returns subsequent rows as dynamic objects.

```csharp
using var reader = new DynamicCsvReader(new StreamReader("products.csv"));
// reader.ForceLowerCaseNames = true;

dynamic row;
while ((row = reader.Read()) != null)
{
    Console.WriteLine($"{row.name}: {row.price}");
}
```

Column names are taken from the first row and sanitized (non-word characters replaced with underscores). Set `ForceLowerCaseNames = true` to normalize all column names to lowercase.

## Type Conversion

`TextClassReader<T>` automatically converts these types: String, Char, Guid, DateTime, Decimal, Double, Single, Int64, Int32, Int16, Byte, Boolean, and Enum types.

---

[Back to documentation index](README.md) | [Project README](../README.md)
