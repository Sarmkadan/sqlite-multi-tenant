# DatabaseController

Provides endpoints for managing and inspecting SQLite multi-tenant databases, including health checks, optimization, integrity verification, schema retrieval, and export functionality.

## API

### `DatabaseController()`
Constructor for the `DatabaseController` class. Initializes a new instance of the controller with default dependency injection.

### `IActionResult GetDatabaseStats()`
Returns statistics about the current database.

- **Returns**: An `IActionResult` containing a `DatabaseStats` object with database metadata.
- **Throws**: May throw if the database is inaccessible or corrupted.

### `async Task<IActionResult> OptimizeDatabase()`
Executes database optimization operations (e.g., `VACUUM`, index maintenance).

- **Returns**: An `IActionResult` containing an `OptimizationResult` with operation details.
- **Throws**: May throw if the database is locked, corrupted, or optimization fails.

### `async Task<IActionResult> CheckIntegrity()`
Validates database integrity by running `PRAGMA integrity_check`.

- **Returns**: An `IActionResult` with a boolean indicating success (`true`) or failure (`false`).
- **Throws**: May throw if the database is inaccessible or the check cannot be performed.

### `IActionResult GetSchema()`
Retrieves the current database schema as a structured object.

- **Returns**: An `IActionResult` containing the schema definition.
- **Throws**: May throw if schema retrieval fails due to permissions or corruption.

### `async Task<IActionResult> ExportDatabase()`
Exports the current database to a file or stream.

- **Returns**: An `IActionResult` with the exported data or a download link.
- **Throws**: May throw if the database is locked, corrupted, or export fails.

## Usage
