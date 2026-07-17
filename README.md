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

## MultiTenantOptionsValidationTests
The `MultiTenantOptionsValidationTests` class provides comprehensive unit tests for validating the configuration options of the multi-tenant SQLite system. These tests verify that the `OptionsValidator.Validate` method correctly validates `MultiTenantOptions`, `BackupOptions`, and `SecurityOptions` classes, ensuring that all required properties contain valid values and that appropriate exceptions are thrown for invalid configurations.

### Public Members

```csharp
public sealed class MultiTenantOptionsValidationTests
public void Validate_MultiTenantOptions_ShouldNotThrowException_WithValidOptions()
public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenBasePathIsEmpty()
public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenMaxConnectionsPerTenantIsZeroOrLess()
public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenMaxBackupCountIsZeroOrLess()
public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenBackupRetentionIsZeroOrLess()
public void Validate_BackupOptions_ShouldNotThrowException_WithValidOptions()
public void Validate_BackupOptions_ShouldThrowArgumentException_WhenMaxConcurrentBackupsIsZeroOrLess()
public void Validate_BackupOptions_ShouldThrowArgumentException_WhenBackupTimeoutSecondsIsZeroOrLess()
public void Validate_SecurityOptions_ShouldNotThrowException_WithValidOptions()
public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenSessionTimeoutIsZeroOrLess()
public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenMaxFailedLoginAttemptsIsZeroOrLess()
public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenLockoutDurationIsZeroOrLess()
```

### Usage Example

```csharp
using SqliteMultiTenant.Configuration;
using System;

class Program
{
  static void Main(string[] args)
  {
    // Example 1: Create valid multi-tenant options
    var validOptions = new MultiTenantOptions
    {
      BasePath = "./databases",
      MaxConnectionsPerTenant = 5,
      MaxBackupCount = 10,
      BackupRetention = TimeSpan.FromDays(30)
    };

    // This should not throw
    OptionsValidator.Validate(validOptions);
    Console.WriteLine("Valid MultiTenantOptions passed validation");

    // Example 2: Create valid backup options
    var validBackupOptions = new BackupOptions
    {
      MaxConcurrentBackups = 2,
      BackupTimeoutSeconds = 300
    };

    // This should not throw
    OptionsValidator.Validate(validBackupOptions);
    Console.WriteLine("Valid BackupOptions passed validation");

    // Example 3: Create valid security options
    var validSecurityOptions = new SecurityOptions
    {
      SessionTimeout = TimeSpan.FromHours(1),
      MaxFailedLoginAttempts = 3,
      LockoutDuration = TimeSpan.FromMinutes(15)
    };

    // This should not throw
    OptionsValidator.Validate(validSecurityOptions);
    Console.WriteLine("Valid SecurityOptions passed validation");

    // Example 4: Test empty BasePath (should throw ArgumentException)
    try
    {
      var invalidOptions = new MultiTenantOptions
      {
        BasePath = "",
        MaxConnectionsPerTenant = 5,
        MaxBackupCount = 10,
        BackupRetention = TimeSpan.FromDays(30)
      };
      OptionsValidator.Validate(invalidOptions);
      Console.WriteLine("ERROR: Should have thrown ArgumentException for empty BasePath");
    }
    catch (ArgumentException ex)
    {
      Console.WriteLine($"Correctly threw ArgumentException: {ex.Message}");
    }

    // Example 5: Test zero MaxConnectionsPerTenant (should throw ArgumentException)
    try
    {
      var invalidOptions = new MultiTenantOptions
      {
        BasePath = "./databases",
        MaxConnectionsPerTenant = 0,
        MaxBackupCount = 10,
        BackupRetention = TimeSpan.FromDays(30)
      };
      OptionsValidator.Validate(invalidOptions);
      Console.WriteLine("ERROR: Should have thrown ArgumentException for zero MaxConnectionsPerTenant");
    }
    catch (ArgumentException ex)
    {
      Console.WriteLine($"Correctly threw ArgumentException: {ex.Message}");
    }

    // Example 6: Test zero MaxConcurrentBackups (should throw ArgumentException)
    try
    {
      var invalidBackupOptions = new BackupOptions
      {
        MaxConcurrentBackups = 0,
        BackupTimeoutSeconds = 300
      };
      OptionsValidator.Validate(invalidBackupOptions);
      Console.WriteLine("ERROR: Should have thrown ArgumentException for zero MaxConcurrentBackups");
    }
    catch (ArgumentException ex)
    {
      Console.WriteLine($"Correctly threw ArgumentException: {ex.Message}");
    }
  }
}

## TenantSettingsEdgeCaseTests
The `TenantSettingsEdgeCaseTests` class provides comprehensive unit tests for the `TenantSettings` model, focusing on edge cases around type conversion, validation boundaries, and error handling. These tests cover validation scenarios for empty IDs, boundary conditions for string lengths, proper error handling for type conversions, and activation state changes, ensuring the tenant settings system operates reliably in multi-tenant SQLite environments.

### Public Members

```csharp
public sealed class TenantSettingsEdgeCaseTests
public void Validate_EmptySettingId_ReturnsError()
public void Validate_EmptyTenantId_ReturnsError()
public void Validate_SettingKeyExceedsMaxLength_ReturnsError()
public void Validate_SettingKeyExactly256Chars_IsValid()
public void GetValue_ValidIntString_ReturnsInt()
public void GetValue_InvalidConversion_ThrowsInvalidOperationException()
public void GetValue_EmptyString_ToInt_ThrowsInvalidOperationException()
public void GetValue_BoolConversion_Works()
public void SetValue_SetsDataTypeToTypeName()
public void UpdateValue_UpdatesTimestampAndModifiedBy()
public void UpdateValue_NullModifiedBy_SetsToNull()
public void SetActive_ToFalse_DeactivatesSetting()
public void SetActive_ToTrue_ActivatesSetting()
```

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Example 1: Create valid tenant settings
        var settings = new TenantSettings
        {
            SettingId = "setting-123",
            TenantId = "tenant-456",
            SettingKey = "max-connections",
            SettingValue = "10"
        };

        // Validate the settings
        var isValid = settings.Validate(out var errors);
        Console.WriteLine(isValid ? "Settings are valid" : "Settings are invalid");

        // Example 2: Test empty SettingId (should return false)
        var emptyIdSettings = new TenantSettings
        {
            SettingId = "",
            TenantId = "tenant-456",
            SettingKey = "key",
            SettingValue = "value"
        };
        
        var isValid2 = emptyIdSettings.Validate(out var errors2);
        Console.WriteLine(!isValid2 && errors2.Any(e => e.Contains("SettingId")) 
            ? "Correctly detected empty SettingId" 
            : "ERROR: Should have detected empty SettingId");

        // Example 3: Test boundary for SettingKey length (exactly 256 chars is valid)
        var maxLengthKey = new string('k', 256);
        var boundarySettings = new TenantSettings
        {
            SettingId = "s1",
            TenantId = "t1",
            SettingKey = maxLengthKey,
            SettingValue = "value"
        };
        
        var isValid3 = boundarySettings.Validate(out var errors3);
        Console.WriteLine(isValid3 ? "256 char SettingKey is valid" : "ERROR: 256 char SettingKey should be valid");

        // Example 4: Test type conversion with GetValue<int>
        var intSettings = new TenantSettings { SettingValue = "42" };
        var intValue = intSettings.GetValue<int>();
        Console.WriteLine(intValue == 42 ? "Int conversion successful" : "ERROR: Int conversion failed");

        // Example 5: Test boolean conversion
        var boolSettings = new TenantSettings { SettingValue = "True" };
        var boolValue = boolSettings.GetValue<bool>();
        Console.WriteLine(boolValue ? "Bool conversion successful" : "ERROR: Bool conversion failed");

        // Example 6: Test SetValue which sets DataType and LastModifiedBy
        var setValueSettings = new TenantSettings();
        setValueSettings.SetValue(123, "admin");
        Console.WriteLine(setValueSettings.DataType == "Int32" && setValueSettings.LastModifiedBy == "admin"
            ? "SetValue works correctly" 
            : "ERROR: SetValue failed");

        // Example 7: Test UpdateValue which updates timestamp and modified by
        var updateSettings = new TenantSettings();
        updateSettings.UpdateValue("new-value", "user1");
        Console.WriteLine(updateSettings.SettingValue == "new-value" && updateSettings.LastModifiedBy == "user1"
            ? "UpdateValue works correctly" 
            : "ERROR: UpdateValue failed");

        // Example 8: Test UpdateValue with null ModifiedBy
        var nullModifiedBySettings = new TenantSettings { LastModifiedBy = "previous" };
        nullModifiedBySettings.UpdateValue("val");
        Console.WriteLine(nullModifiedBySettings.LastModifiedBy == null
            ? "UpdateValue with null ModifiedBy works correctly" 
            : "ERROR: UpdateValue with null ModifiedBy failed");

        // Example 9: Test SetActive to deactivate
        var inactiveSettings = new TenantSettings { IsActive = true };
        inactiveSettings.SetActive(false);
        Console.WriteLine(!inactiveSettings.IsActive
            ? "SetActive(false) works correctly" 
            : "ERROR: SetActive(false) failed");

        // Example 10: Test SetActive to activate
        var activeSettings = new TenantSettings { IsActive = false };
        activeSettings.SetActive(true);
        Console.WriteLine(activeSettings.IsActive
            ? "SetActive(true) works correctly" 
            : "ERROR: SetActive(true) failed");
    }
}
```
