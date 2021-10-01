# DataExporter
The `DataExporter` class is a utility for exporting data from a SQLite database in various formats. It provides methods for exporting data as JSON, CSV, and SQL, allowing for flexible data interchange and manipulation. This class is designed to be used in a multi-tenant environment, where data needs to be exported and imported across different databases.

## API
The `DataExporter` class has the following public members:
* `DataExporter`: The constructor for the `DataExporter` class.
* `ExportAsJsonAsync`: Exports the data as a JSON string. This method is asynchronous and returns a `Task<string>`. It throws an exception if an error occurs during the export process.
* `ExportAsCsvAsync`: Exports the data as a CSV string. This method is asynchronous and returns a `Task<string>`. It throws an exception if an error occurs during the export process.
* `ExportAsSqlAsync`: Exports the data as a SQL string. This method is asynchronous and returns a `Task<string>`. It throws an exception if an error occurs during the export process.

## Usage
Here are two examples of using the `DataExporter` class:
```csharp
// Example 1: Exporting data as JSON
var exporter = new DataExporter();
var jsonData = await exporter.ExportAsJsonAsync();
Console.WriteLine(jsonData);

// Example 2: Exporting data as CSV
var exporter = new DataExporter();
var csvData = await exporter.ExportAsCsvAsync();
File.WriteAllText("data.csv", csvData);
```

## Notes
The `DataExporter` class is designed to be thread-safe, allowing it to be used concurrently from multiple threads. However, the export methods are asynchronous and may throw exceptions if errors occur during the export process. It is recommended to handle these exceptions accordingly to ensure robust error handling. Additionally, the export methods may return large strings, which can impact memory usage. It is recommended to use streaming or other techniques to mitigate this impact if necessary.
