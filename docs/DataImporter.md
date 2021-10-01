# DataImporter

The `DataImporter` class provides a set of asynchronous methods for importing data into a SQLite-based multi-tenant database from various sources, including JSON, CSV, and SQL databases. It is designed to handle bulk data operations efficiently, returning the count of successfully imported records.

## API

### `DataImporter()`
Constructs a new instance of the `DataImporter` class. This constructor initializes the importer with default settings for data processing.

### `Task<int> ImportFromJsonAsync`
Imports data from a JSON source into the target database.

**Parameters:**
- None (assumed to be configured via internal mechanisms or properties not exposed in the public API).

**Returns:**
- A `Task<int>` representing the number of records successfully imported.

**Throws:**
- `ArgumentException`: If the JSON source is malformed or incompatible with the target schema.
- `InvalidOperationException`: If the import operation cannot proceed due to database constraints or connectivity issues.
- `SqlException`: If an underlying database error occurs during the import process.

---

### `Task<int> ImportFromCsvAsync`
Imports data from a CSV source into the target database.

**Parameters:**
- None (assumed to be configured via internal mechanisms or properties not exposed in the public API).

**Returns:**
- A `Task<int>` representing the number of records successfully imported.

**Throws:**
- `ArgumentException`: If the CSV source is malformed or incompatible with the target schema.
- `InvalidOperationException`: If the import operation cannot proceed due to database constraints or connectivity issues.
- `SqlException`: If an underlying database error occurs during the import process.

---

### `Task<int> ImportFromSqlAsync`
Imports data from an external SQL database into the target SQLite database.

**Parameters:**
- None (assumed to be configured via internal mechanisms or properties not exposed in the public API).

**Returns:**
- A `Task<int>` representing the number of records successfully imported.

**Throws:**
- `ArgumentException`: If the SQL source query is invalid or the result set is incompatible with the target schema.
- `InvalidOperationException`: If the import operation cannot proceed due to database constraints, connectivity issues, or schema mismatches.
- `SqlException`: If an underlying database error occurs during the import process.

## Usage

### Example 1: Importing Data from JSON
