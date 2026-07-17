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

## TenantRepositoryIntegrationTests
The `TenantRepositoryIntegrationTests` class provides integration tests for the `TenantRepository` class, verifying that CRUD operations work correctly. These tests cover retrieving all tenants, getting a tenant by ID, adding a new tenant, updating an existing tenant, and deleting a tenant. 

### Usage Example
```csharp
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Tests;
using System;

class Program
{
    static async Task Main(string[] args)
    {
        var tests = new TenantRepositoryIntegrationTests();
        await tests.GetAllAsync_ShouldReturnAllTenants();
        await tests.GetByIdAsync_ShouldReturnCorrectTenant_WhenTenantExists();
        await tests.GetByIdAsync_ShouldReturnNull_WhenTenantDoesNotExist();
        await tests.AddAsync_ShouldAddTenantToDatabase();
        await tests.UpdateAsync_ShouldUpdateTenantInDatabase();
        await tests.DeleteAsync_ShouldRemoveTenantFromDatabase();
        tests.Dispose();
    }
}
```
## ConnectionPoolOptionsTests
The `ConnectionPoolOptionsTests` class provides unit tests for the `ConnectionPoolOptions` class, verifying that connection pool configuration validation works correctly. These tests validate various scenarios including valid configurations, boundary conditions, and error handling for invalid inputs such as negative values, zero values, and invalid ranges.

### Public Members

```csharp
public sealed class ConnectionPoolOptionsTests
public void Validate_WithValidOptions_DoesNotThrow()
public void Validate_WithMinPoolSizeZero_DoesNotThrow()
public void Validate_ThrowsArgumentOutOfRangeException_WhenMinPoolSizeIsNegative()
public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxPoolSizeIsZero()
public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxPoolSizeIsNegative()
public void Validate_ThrowsArgumentException_WhenMinPoolSizeExceedsMaxPoolSize()
public void Validate_ThrowsArgumentOutOfRangeException_WhenIdleTimeoutIsZero()
public void Validate_ThrowsArgumentOutOfRangeException_WhenAcquireTimeoutIsZero()
public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxConnectionLifetimeIsZero()
public void Validate_ThrowsArgumentOutOfRangeException_WhenPruneIntervalIsZero()
```

### Usage Example

```csharp
using SqliteMultiTenant.Database;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Example 1: Create valid connection pool options
        var validOptions = new ConnectionPoolOptions
        {
            MinPoolSize = 1,
            MaxPoolSize = 10,
            IdleTimeout = TimeSpan.FromMinutes(5),
            AcquireTimeout = TimeSpan.FromSeconds(30),
            MaxConnectionLifetime = TimeSpan.FromHours(1),
            PruneInterval = TimeSpan.FromSeconds(60)
        };
        
        // This should not throw
        validOptions.Validate();
        Console.WriteLine("Valid options passed validation");
        
        // Example 2: Test with MinPoolSize = 0 (valid)
        var zeroMinOptions = new ConnectionPoolOptions
        {
            MinPoolSize = 0,
            MaxPoolSize = 1,
            IdleTimeout = TimeSpan.FromMinutes(1),
            AcquireTimeout = TimeSpan.FromSeconds(1),
            MaxConnectionLifetime = TimeSpan.FromHours(1),
            PruneInterval = TimeSpan.FromSeconds(1)
        };
        zeroMinOptions.Validate();
        Console.WriteLine("Zero MinPoolSize passed validation");
        
        // Example 3: Test with negative MinPoolSize (should throw)
        try
        {
            var negativeMinOptions = new ConnectionPoolOptions { MinPoolSize = -1 };
            negativeMinOptions.Validate();
            Console.WriteLine("ERROR: Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Correctly threw ArgumentOutOfRangeException: {ex.Message}");
        }
        
        // Example 4: Test with MinPoolSize > MaxPoolSize (should throw)
        try
        {
            var invalidRangeOptions = new ConnectionPoolOptions { MinPoolSize = 10, MaxPoolSize = 5 };
            invalidRangeOptions.Validate();
            Console.WriteLine("ERROR: Should have thrown ArgumentException");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Correctly threw ArgumentException: {ex.Message}");
        }
        
        // Example 5: Test with zero IdleTimeout (should throw)
        try
        {
            var zeroIdleOptions = new ConnectionPoolOptions { IdleTimeout = TimeSpan.Zero };
            zeroIdleOptions.Validate();
            Console.WriteLine("ERROR: Should have thrown ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"Correctly threw ArgumentOutOfRangeException: {ex.Message}");
        }
    }
}
```
