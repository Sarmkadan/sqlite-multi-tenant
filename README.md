
## DataConsistencyCheckerTests

The `DataConsistencyCheckerTests` class provides comprehensive unit tests for the `DataConsistencyChecker` class, verifying that database integrity checking operations work correctly. These tests cover validation scenarios, error handling for invalid inputs, and proper error handling for database connectivity issues, ensuring the data consistency checker operates reliably for multi-tenant SQLite systems.

### Public Members

```csharp
public sealed class DataConsistencyCheckerTests
public DataConsistencyCheckerTests()
public async Task CheckDatabaseIntegrityAsync_WithClosedConnection_ThrowsException()
public async Task CheckDatabaseIntegrityAsync_WithValidMemoryConnection_ReturnsHealthy()
public async Task CheckDatabaseIntegrityAsync_WithNullConnection_ThrowsArgumentNullException()
public void Checker_Initialization_WithNullLogger_ThrowsArgumentNullException()
public async Task CheckDatabaseIntegrityAsync_WithCorruptData_SimulatesFailure()
```

### Usage Example

```csharp
using SqliteMultiTenant.DataOperations;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System;

// Create logger and checker instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<DataConsistencyChecker>();
var consistencyChecker = new DataConsistencyChecker(logger);

// Example 1: Check database integrity with a valid connection
using var validConnection = new SQLiteConnection("Data Source=:memory:");
await validConnection.OpenAsync();
var integrityResult = await consistencyChecker.CheckDatabaseIntegrityAsync(validConnection);
Console.WriteLine(integrityResult != null ? "Database integrity check passed" : "Database integrity check failed");

// Example 2: Handle null connection (should throw ArgumentNullException)
try
{
    await consistencyChecker.CheckDatabaseIntegrityAsync(null!);
    Console.WriteLine("ERROR: Should have thrown ArgumentNullException");
}
catch (ArgumentNullException)
{
    Console.WriteLine("Correctly threw ArgumentNullException for null connection");
}

// Example 3: Handle closed connection (should throw Exception)
using var closedConnection = new SQLiteConnection("Data Source=:memory:");
// Connection is intentionally not opened
try
{
    await consistencyChecker.CheckDatabaseIntegrityAsync(closedConnection);
    Console.WriteLine("ERROR: Should have thrown Exception for closed connection");
}
catch (Exception)
{
    Console.WriteLine("Correctly threw Exception for closed connection");
}

// Example 4: Initialize checker with null logger (should throw ArgumentNullException)
try
{
    var invalidChecker = new DataConsistencyChecker(null!);
    Console.WriteLine("ERROR: Should have thrown ArgumentNullException for null logger");
}
catch (ArgumentNullException)
{
    Console.WriteLine("Correctly threw ArgumentNullException for null logger");
}

// Example 5: Simulate data corruption scenario
using var testConnection = new SQLiteConnection("Data Source=:memory:");
await testConnection.OpenAsync();
var corruptionResult = await consistencyChecker.CheckDatabaseIntegrityAsync(testConnection);
Console.WriteLine(corruptionResult != null ? "Corruption check completed" : "Corruption check failed");
```