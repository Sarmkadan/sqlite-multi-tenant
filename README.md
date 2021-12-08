// existing content ...

## DataImporterExtensions

The `DataImporterExtensions` class provides a set of extension methods for importing data into the database. It supports importing data from JSON, CSV, and SQL files, as well as validating and creating tables if they do not exist.

### Usage Example

```csharp
var jsonFilePath = "path/to/data.json";
var csvFilePath = "path/to/data.csv";
var sqlFilePath = "path/to/data.sql";

var jsonImportResult = await DataImporterExtensions.ImportFromJsonFileAsync(jsonFilePath);
var csvImportResult = await DataImporterExtensions.ImportFromCsvFileAsync(csvFilePath);
var sqlImportResult = await DataImporterExtensions.ImportFromSqlFileAsync(sqlFilePath);

var tableExists = await DataImporterExtensions.ValidateTableExistsAsync("TableName");
if (!tableExists)
{
    await DataImporterExtensions.CreateTableIfNotExistsAsync("TableName");
}
```

// existing content ...
