# Output Formatters

Documentation for the output formatting system in the SqliteMultiTenant project.

## Overview

The output formatting system provides pluggable serialization for API responses and file exports in JSON, CSV, and XML formats. The system consists of:

- `IOutputFormatter` interface defining the contract
- Concrete formatters: `JsonExportFormatter`, `CsvExportFormatter`, `XmlExportFormatter`
- `FormatterFactory` for selecting formatters by type or content type
- `OutputFormatter` for general-purpose formatting used by CLI output paths

## IOutputFormatter

Located in: `src/Formatters/OutputFormatter.cs`

Defines the interface for all output formatters:

```csharp
public interface IOutputFormatter
{
    string Format<T>(T data);
    string ContentType { get; }
}
```

## JsonExportFormatter

Located in: `src/Formatters/JsonFormatter.cs`

Formats objects as JSON with customizable serialization options.

### Constructor Parameters
- `ILogger<JsonExportFormatter> logger` - Logger instance
- `bool prettyPrint = true` - Whether to format JSON with indentation

### Key Methods
- `string Format<T>(T? data)` - Serializes object to JSON string
- `T? Parse<T>(string json)` - Deserializes JSON string to object
- `string FormatWithOptions<T>(T? data, JsonSerializerOptions options)` - Formats with custom options
- `static JsonSerializerOptions GetMinimalOptions()` - Creates options for compact JSON
- `static JsonSerializerOptions GetVerboseOptions()` - Creates options for verbose JSON

### Example Usage
```csharp
var formatter = new JsonExportFormatter(logger, prettyPrint: true);
var json = formatter.Format(new { Name = "John", Age = 30 });
// Returns: {
//   "name": "John",
//   "age": 30
// }
```

## CsvExportFormatter

Located in: `src/Formatters/CsvFormatter.cs`

Formats objects and collections as CSV (Comma-Separated Values).

### Constructor Parameters
- `ILogger<CsvExportFormatter> logger` - Logger instance
- `string delimiter = ","` - Field delimiter character
- `bool includeHeader = true` - Whether to include column headers

### Key Methods
- `string Format<T>(T? data)` - Formats object or collection as CSV

### Example Usage
```csharp
var formatter = new CsvExportFormatter(logger, delimiter: ",", includeHeader: true);
var data = new[] {
    new { Name = "John", Age = 30 },
    new { Name = "Jane", Age = 25 }
};
var csv = formatter.Format(data);
// Returns:
// Name,Age
// John,30
// Jane,25
```

## XmlExportFormatter

Located in: `src/Formatters/XmlFormatter.cs`

Formats objects and collections as XML.

### Constructor Parameters
- `ILogger<XmlExportFormatter> logger` - Logger instance
- `bool includeDeclaration = true` - Whether to include XML declaration

### Key Methods
- `string Format<T>(T? data, string rootName = "root")` - Formats object or collection as XML

### Example Usage
```csharp
var formatter = new XmlExportFormatter(logger, includeDeclaration: true);
var data = new { Name = "John", Age = 30 };
var xml = formatter.Format(data, "Person");
// Returns:
// <?xml version="1.0" encoding="utf-8" standalone="yes"?>
// <Person>
//   <name>John</name>
//   <age>30</age>
// </Person>
```

## FormatterFactory

Located in: `src/Formatters/OutputFormatter.cs` (inner class)

Factory for selecting formatter based on content type or file extension.

### Usage
```csharp
var factory = new FormatterFactory();
IOutputFormatter jsonFormatter = factory.GetFormatter("json");
IOutputFormatter csvFormatter = factory.GetFormatterByContentType("text/csv");
```

## OutputFormatter

Located in: `src/Formatters/OutputFormatter.cs` (outer class)

General-purpose formatter used by CLI output paths.

### Usage
```csharp
var formatter = new OutputFormatter();
string text = formatter.FormatObject(new { Name = "John" }, "text");
// Returns:
// Name: John
string json = formatter.FormatObject(new { Name = "John" }, "json");
// Returns JSON formatted string
```