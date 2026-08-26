## IntegrityCheckService

The `IntegrityCheckService` provides functionality to perform SQLite `PRAGMA integrity_check` operations across tenant databases. It supports checking individual tenants, batches of tenants, or all tenants in the system, with configurable parallelism to manage system load during validation.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using System.Threading;
using System.Threading.Tasks;

// Assume 'service' is an instance of IntegrityCheckService
// Assume 'cancellationToken' is available

// 1. Check integrity for a single tenant
TenantIntegrityCheckResult result = await service.CheckTenantIntegrityAsync("tenant-123", cancellationToken);
Console.WriteLine($"Tenant: {result.TenantId}, IsOk: {result.IsOk}");

// 2. Check integrity for a specific list of tenants with parallelism
List<string> tenantsToCheck = new List<string> { "tenant-123", "tenant-456" };
List<TenantIntegrityCheckResult> batchResults = await service.CheckTenantsIntegrityAsync(tenantsToCheck, maxDegreeOfParallelism: 2, cancellationToken);

// 3. Check integrity for all tenants in the system
List<TenantIntegrityCheckResult> allResults = await service.CheckAllTenantsIntegrityAsync(maxDegreeOfParallelism: 4, cancellationToken);

// 4. Check integrity for only active tenants
List<TenantIntegrityCheckResult> activeResults = await service.CheckActiveTenantsIntegrityAsync(maxDegreeOfParallelism: 4, cancellationToken);
```

## TenantContextHelperExtensions

The `TenantContextHelperExtensions` class provides a set of extension methods for `TenantContextHelper` that simplify common operations within multi-tenant scopes. It includes utilities for managing tenant-aware scopes, retrieving tenant information safely, and executing actions within specific tenant contexts.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Models;
using System;

// Assume 'helper' is an instance of TenantContextHelper
var helper = new TenantContextHelper(); 
string tenantId = "tenant-123";

// 1. Check if the current tenant is the target
if (helper.IsCurrentTenant(tenantId))
{
    Console.WriteLine("Already in the target tenant context.");
}

// 2. Create a validated scope and perform operations
using (helper.CreateValidatedScope(tenantId, userId: "user-456"))
{
    // 3. Retrieve context information
    var context = helper.GetRequiredTenantContext();
    Console.WriteLine($"Current Tenant: {context.TenantId}");
}

// 4. Get the required tenant ID directly
string currentId = helper.GetRequiredTenantId();

// 5. Execute an action within a tenant context
helper.ExecuteInTenantContext(tenantId, () => {
    Console.WriteLine("Executing action inside tenant context...");
});

// 6. Execute a function within a tenant context and get a result
int itemCount = helper.ExecuteInTenantContext(tenantId, () => {
    return 42; // Example return value
});
```


The `RateLimitingMiddlewareValidation` class provides validation utilities for rate limiting middleware components in multi-tenant SQLite environments. It offers methods to validate rate limiting configurations, middleware instances, and related rate limiting data structures, ensuring proper rate limit enforcement and configuration correctness.

### Usage Example

```csharp
using SqliteMultiTenant.Middleware;
using System;
using System.Collections.Generic;

// Example 1: Validate a RateLimitingMiddleware instance
var middleware = new RateLimitingMiddleware(
    maxRequestsPerSecond: 100,
    maxBurst: 200,
    windowSize: TimeSpan.FromMinutes(1)
);

// Validate the middleware instance
IReadOnlyList<string> middlewareProblems = RateLimitingMiddlewareValidation.Validate(middleware);
if (middlewareProblems.Count > 0)
{
    Console.WriteLine("RateLimitingMiddleware has problems:");
    foreach (var p in middlewareProblems) Console.WriteLine($"- {p}");
}
else
{
    Console.WriteLine("RateLimitingMiddleware instance is valid.");
}

// Shortcut to just get a boolean result
bool isValid = RateLimitingMiddlewareValidation.IsValid(middleware);
Console.WriteLine($"IsValid: {isValid}");

// Throw an exception if the middleware instance is not valid
RateLimitingMiddlewareValidation.EnsureValid(middleware);

// Example 2: Validate a RateLimitingConfig configuration
var rateLimitConfig = new RateLimitingConfig
{
    MaxRequestsPerSecond = 50,
    MaxBurst = 100,
    WindowSize = TimeSpan.FromSeconds(30),
    Enabled = true,
    BanDuration = TimeSpan.FromMinutes(5)
};

IReadOnlyList<string> configProblems = RateLimitingMiddlewareValidation.Validate(rateLimitConfig);
if (configProblems.Count > 0)
{
    Console.WriteLine("RateLimitingConfig has problems:");
    foreach (var p in configProblems) Console.WriteLine($"- {p}");
}
else
{
    Console.WriteLine("RateLimitingConfig is valid.");
}

// Example 3: Validate a RateLimitExceededResult from rate limiting operations
var exceededResult = new RateLimitExceededResult
{
    IsExceeded = true,
    RetryAfter = TimeSpan.FromSeconds(15),
    CurrentRequestCount = 150,
    Limit = 100,
    Window = TimeSpan.FromMinutes(1)
};

IReadOnlyList<string> exceededProblems = RateLimitingMiddlewareValidation.Validate(exceededResult);
Console.WriteLine(exceededProblems.Count == 0
    ? "RateLimitExceededResult is valid."
    : $"RateLimitExceededResult problems: {string.Join(", ", exceededProblems)}");

// Example 4: Validate a RateLimitStatistics instance for monitoring
var stats = new RateLimitStatistics
{
    TotalRequests = 1000,
    AllowedRequests = 950,
    DeniedRequests = 50,
    PeakRequestsPerSecond = 120,
    CurrentActiveLimits = 42
};

IReadOnlyList<string> statsProblems = RateLimitingMiddlewareValidation.Validate(stats);
Console.WriteLine(statsProblems.Count == 0
    ? "RateLimitStatistics is valid."
    : $"RateLimitStatistics problems: {string.Join(", ", statsProblems)}");

// Example 5: Using validation in a rate limiting middleware workflow
try
{
    // Initialize middleware with configuration
    var rateLimitingMiddleware = new RateLimitingMiddleware(
        maxRequestsPerSecond: 100,
        maxBurst: 200,
        windowSize: TimeSpan.FromMinutes(1)
    );
    
    // Validate configuration before use
    RateLimitingMiddlewareValidation.EnsureValid(rateLimitingMiddleware);
    
    Console.WriteLine("Rate limiting middleware is properly configured and ready to use.");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Rate limiting middleware validation failed: {ex.Message}");
}
```

## ErrorHandlingMiddlewareValidation

`ErrorHandlingMiddlewareValidation` provides a set of extension methods that validate an `ErrorHandlingMiddleware` instance and `Result<T>` objects. It checks for null references, consistency between success flags and error messages, and ensures that successful results contain a non‑default value.

### Usage Example

```csharp
using SqliteMultiTenant.Middleware;
using System;
using System.Collections.Generic;

public void ProcessMiddleware(ErrorHandlingMiddleware middleware)
{
    // Throw an exception if the middleware is invalid
    middleware.EnsureValid();

    // Or just check validity
    if (!middleware.IsValid())
    {
        Console.WriteLine("ErrorHandlingMiddleware has validation problems:");
        foreach (var p in middleware.Validate())
        {
            Console.WriteLine($"- {p}");
        }
    }
    else
    {
        Console.WriteLine("ErrorHandlingMiddleware instance is valid.");
    }
}

// Example with a Result<T>
Result<string> result = GetResult(); // Assume this returns a Result<string>

result.EnsureValid(); // Throws if invalid

if (result.IsValid())
{
    Console.WriteLine($"Operation succeeded with value: {result.Value}");
}
else
{
    Console.WriteLine($"Operation failed: {result.ErrorMessage}");
}
```

These helpers make it easy to enforce invariants and surface configuration problems early in the request pipeline.


## SettingsControllerExtensions

The `SettingsControllerExtensions` class provides a collection of extension methods for `SettingsController` that simplify common operations when working with application settings. These methods offer strongly-typed access to settings, batch operations, and filtering capabilities, reducing boilerplate code and making settings management more intuitive.



### Usage Examples

```csharp
using Microsoft.AspNetCore.Mvc;
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Responses;
using System;
using System.Collections.Generic;

// Example 1: Get a setting as a specific type
var controller = new SettingsController();
IActionResult result = controller.GetSettingAs<int>("max_connections");
if (result is OkObjectResult okResult && okResult.Value is ApiResponse<int> response)
{
    Console.WriteLine($"Max connections: {response.Data}");
}

// Example 2: Set a setting with a strongly-typed value
result = controller.SetSetting("timeout_seconds", 30);

// Example 3: Update multiple settings in a batch operation
var settings = new Dictionary<string, string>
{
    {"theme", "dark"},
    {"language", "en-US"},
    {"items_per_page", "25"}
};
result = controller.UpdateBatchSettings(settings);

// Example 4: Check if a setting exists
result = controller.SettingExists("maintenance_mode");
if (result is OkObjectResult existsResult && existsResult.Value is ApiResponse<bool> existsResponse)
{
    Console.WriteLine($"Setting exists: {existsResponse.Data}");
}

// Example 5: Get settings filtered by a predicate
result = controller.GetSettingsWhere(setting => setting.Key.StartsWith("app_"));
if (result is OkObjectResult filteredResult && filteredResult.Value is ApiResponse<IReadOnlyList<SettingValue>> filteredResponse)
{
    foreach (var setting in filteredResponse.Data)
    {
        Console.WriteLine($"{setting.Key}: {setting.Value}");
    }
}

// Example 6: Get a setting with custom parsing
result = controller.GetSettingAs<DateTime>("last_backup", (value, type) => DateTime.Parse(value));
```


## ReportGeneratorJsonExtensions

The `ReportGeneratorJsonExtensions` class provides convenient extension methods for serializing and deserializing report-related data structures using `System.Text.Json`. It simplifies converting monitoring objects like health summaries, operation statistics, and performance metrics to and from JSON strings, ensuring consistent configuration and error handling.

### Usage Example

```csharp
using SqliteMultiTenant.Monitoring;
using System;
using System.Collections.Generic;

// Example 1: Serialize a SystemHealthSummary
var healthSummary = new SystemHealthSummary { /* Initialize properties */ };
string json = healthSummary.ToJson();
Console.WriteLine($"Serialized Health Summary: {json}");

// Example 2: Deserialize from JSON string
string jsonStats = "[...]"; // JSON string of OperationStatistics collection
var stats = ReportGeneratorJsonExtensions.FromJsonToOperationStatistics(jsonStats);

// Example 3: Try deserialization with error handling
string jsonMetric = "{...}"; // JSON string of PerformanceMetric
if (jsonMetric.TryFromJson(out IEnumerable<PerformanceMetric>? metrics))
{
    Console.WriteLine($"Successfully deserialized {metrics?.Count()} metrics.");
}
else
{
    Console.WriteLine("Failed to deserialize performance metrics.");
}
```

## StringUtilitiesJsonExtensions

The `StringUtilitiesJsonExtensions` class provides convenient extension methods for serializing and deserializing string data using `System.Text.Json`. It includes specialized utilities to handle JSON serialization with additional metadata like SHA256 hashes or snake_case conversions, as well as safe deserialization methods.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

string originalValue = "Hello World";

// 1. Serialize to JSON
string json = originalValue.ToJson();
Console.WriteLine($"JSON: {json}");

// 2. Deserialize from JSON
string? deserializedValue = json.FromJson();
Console.WriteLine($"Deserialized: {deserializedValue}");

// 3. Try deserialize with error handling
if (json.TryFromJson(out string? value))
{
    Console.WriteLine($"Successfully deserialized: {value}");
}

// 4. Serialize with SHA256 hash
string jsonWithHash = originalValue.ToJsonWithHash();
Console.WriteLine($"JSON with Hash: {jsonWithHash}");

// 5. Serialize with snake_case conversion
string jsonWithSnakeCase = originalValue.ToJsonWithSnakeCase();
Console.WriteLine($"JSON with SnakeCase: {jsonWithSnakeCase}");
```

## TenantIsolationEnforcementTests

The `TenantIsolationEnforcementTests` class contains end-to-end tests that verify tenant data isolation guarantees in a multi-tenant SQLite environment. It exercises two isolation strategies:
- **Connection-per-tenant**: Each tenant uses its own physical SQLite file, providing complete file-level isolation.
- **Shared-schema**: All tenants share a single SQLite file with a `TenantId` discriminator column that scopes every query.

These tests ensure that a tenant can never read, update, or delete another tenant's rows, even when using deliberately hostile queries.

### Usage Example

```csharp
using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Xunit;

## TenantNameValidatorTestsExtensions

The `TenantNameValidatorTestsExtensions` class provides extension methods for testing tenant name validation logic. It includes methods to verify that tenant names are correctly converted to tenant IDs and to validate both valid and invalid tenant name scenarios.

### Usage Example

```csharp
using SqliteMultiTenant.Validation;
using System;
using System.Linq;

// Example 1: Test that a valid tenant name generates the expected tenant ID
"MyTenant".ShouldGenerateTenantId("mytenant");

// Example 2: Verify that a tenant name is considered valid
"valid-tenant-name".ShouldBeValidTenantId();

// Example 3: Verify that an invalid tenant name produces the expected error
"INVALID tenant!".ShouldBeInvalidTenantIdWithError("Tenant name contains invalid characters");

// Example 4: Get all invalid tenant IDs with their expected errors
var invalidTenantIds = TenantNameValidatorTestsExtensions.GetInvalidTenantIds();
foreach (var (tenantId, expectedError) in invalidTenantIds)
{
    Console.WriteLine($"Tenant ID: {tenantId}, Expected Error: {expectedError}");
}

// Example 5: Get all valid tenant name mappings with their expected tenant IDs
var validMappings = TenantNameValidatorTestsExtensions.GetValidTenantNameMappings();
foreach (var (tenantName, expectedTenantId) in validMappings)
{
    Console.WriteLine($"Tenant Name: {tenantName} -> Tenant ID: {expectedTenantId}");
}
```

## TenantNameValidatorTestsExtensions

The `TenantNameValidatorTestsExtensions` class provides extension methods for testing tenant name validation logic. It includes methods to verify that tenant names are correctly converted to tenant IDs and to validate both valid and invalid tenant name scenarios.

### Usage Example

```csharp
using SqliteMultiTenant.Validation;
using System;
using System.Linq;

// Example 1: Test that a valid tenant name generates the expected tenant ID
"MyTenant".ShouldGenerateTenantId("mytenant");

// Example 2: Verify that a tenant name is considered valid
"valid-tenant-name".ShouldBeValidTenantId();

// Example 3: Verify that an invalid tenant name produces the expected error
"INVALID tenant!".ShouldBeInvalidTenantIdWithError("Tenant name contains invalid characters");

// Example 4: Get all invalid tenant IDs with their expected errors
var invalidTenantIds = TenantNameValidatorTestsExtensions.GetInvalidTenantIds();
foreach (var (tenantId, expectedError) in invalidTenantIds)
{
    Console.WriteLine($"Tenant ID: {tenantId}, Expected Error: {expectedError}");
}

// Example 5: Get all valid tenant name mappings with their expected tenant IDs
var validMappings = TenantNameValidatorTestsExtensions.GetValidTenantNameMappings();
foreach (var (tenantName, expectedTenantId) in validMappings)
{
    Console.WriteLine($"Tenant Name: {tenantName} -> Tenant ID: {expectedTenantId}");
}
```

// Example 1: Verify connection-per-tenant isolation
public async Task TestConnectionPerTenantIsolation()
{
    // Create separate database files for each tenant
    var pathA = Path.Combine(Path.GetTempPath(), $"tenant_a_{Guid.NewGuid():N}.db");
    var pathB = Path.Combine(Path.GetTempPath(), $"tenant_b_{Guid.NewGuid():N}.db");
    
    // Create documents table in each tenant's database
    await CreateDocumentsTableAsync(Conn(pathA));
    await CreateDocumentsTableAsync(Conn(pathB));
    
    // Insert tenant-specific data
    await InsertDocumentAsync(Conn(pathA), 1, "tenant-a", "A-invoice.pdf");
    await InsertDocumentAsync(Conn(pathB), 1, "tenant-b", "B-invoice.pdf");
    
    // Tenant A can only see its own data
    var tenantAData = await ReadTitlesForTenantAsync(Conn(pathA), "tenant-a");
    Assert.Contains("A-invoice.pdf", tenantAData);
    Assert.DoesNotContain("B-invoice.pdf", tenantAData);
    
    // Tenant B can only see its own data
    var tenantBData = await ReadTitlesForTenantAsync(Conn(pathB), "tenant-b");
    Assert.Contains("B-invoice.pdf", tenantBData);
    Assert.DoesNotContain("A-invoice.pdf", tenantBData);
}

// Example 2: Verify shared-schema isolation
public async Task TestSharedSchemaIsolation()
{
    var sharedPath = Path.Combine(Path.GetTempPath(), $"shared_{Guid.NewGuid():N}.db");
    await CreateDocumentsTableAsync(Conn(sharedPath));
    
    // Insert data for multiple tenants in the same database
    await InsertDocumentAsync(Conn(sharedPath), 1, "tenant-1", "Document 1");
    await InsertDocumentAsync(Conn(sharedPath), 2, "tenant-2", "Document 2");
    
    // Each tenant only sees its own rows when querying with TenantId filter
    var tenant1Data = await ReadTitlesForTenantAsync(Conn(sharedPath), "tenant-1");
    Assert.Contains("Document 1", tenant1Data);
    Assert.DoesNotContain("Document 2", tenant1Data);
    
    var tenant2Data = await ReadTitlesForTenantAsync(Conn(sharedPath), "tenant-2");
    Assert.Contains("Document 2", tenant2Data);
    Assert.DoesNotContain("Document 1", tenant2Data);
}

// Helper methods
private static string Conn(string path) => $"Data Source={path};Version=3;";

private static async Task CreateDocumentsTableAsync(string connectionString)
{
    using var conn = new SQLiteConnection(connectionString);
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Documents (
            Id INTEGER PRIMARY KEY,
            TenantId TEXT NOT NULL,
            Title TEXT NOT NULL
        );";
    await cmd.ExecuteNonQueryAsync();
}

private static async Task InsertDocumentAsync(string connectionString, int id, string tenantId, string title)
{
    using var conn = new SQLiteConnection(connectionString);
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Documents (Id, TenantId, Title) VALUES (@id, @tid, @title)";
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@tid", tenantId);
    cmd.Parameters.AddWithValue("@title", title);
    await cmd.ExecuteNonQueryAsync();
}

private static async Task System.Collections.Generic.List<string> ReadTitlesForTenantAsync(string connectionString, string tenantId)
{
    using var conn = new SQLiteConnection(connectionString);
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Title FROM Documents WHERE TenantId = @tid ORDER BY Id";
    cmd.Parameters.AddWithValue("@tid", tenantId);
    
    var titles = new System.Collections.Generic.List<string>();
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        titles.Add(reader.GetString(0));
    }
    return titles;
}
```

## DataValidatorExtensions

The `DataValidatorExtensions` class provides a comprehensive set of extension methods for validating various data types and collections. It includes validation methods for strings, collections, and common data formats like phone numbers, dates, times, IP addresses, and credit cards. These validators help ensure data integrity by checking length constraints, format validity, and value ranges.

### Usage Example

```csharp
using SqliteMultiTenant.Validation;
using System;
using System.Collections.Generic;

// Example 1: Validate string requirements
var userName = "john_doe";
var nameValidation = userName.RequireString("username", minLength: 3, maxLength: 50);
if (nameValidation.IsValid)
{
    Console.WriteLine("Username is valid.");
}
else
{
    Console.WriteLine($"Username validation failed: {string.Join(", ", nameValidation.Errors)}");
}

// Example 2: Validate string length constraints
var password = "SecurePass123!";
var passwordValidation = password.RequireMinLength("password", 8);
if (!passwordValidation.IsValid)
{
    Console.WriteLine("Password must be at least 8 characters long.");
}

// Example 3: Validate collection count
var tags = new List<string> { "tag1", "tag2", "tag3" };
var tagsValidation = tags.RequireCollectionCount("tags", minCount: 1, maxCount: 10);
if (tagsValidation.IsValid)
{
    Console.WriteLine($"Tags collection is valid with {tags.Count} items.");
}

// Example 4: Validate date and time
var birthDate = new DateTime(1990, 5, 15);
var dateValidation = birthDate.RequireValidDate("birthDate");
if (dateValidation.IsValid)
{
    Console.WriteLine($"Birth date is valid: {birthDate:yyyy-MM-dd}");
}

// Example 5: Validate IP address
var ipAddress = "192.168.1.1";
var ipValidation = ipAddress.RequireValidIPv4("ipAddress");
if (ipValidation.IsValid)
{
    Console.WriteLine("IP address is valid.");
}

// Example 6: Validate with custom error handling
var email = "user@example.com";
var emailValidation = email.RequireString("email", minLength: 5, maxLength: 100);
if (!emailValidation.IsValid)
{
    throw new ArgumentException($"Invalid email: {string.Join(", ", emailValidation.Errors)}");
}



## TenantSettingsEdgeCaseTestsExtensions

The `TenantSettingsEdgeCaseTestsExtensions` class provides a suite of extension methods designed for testing `TenantSettings` under various conditions, including data type conversion, validation scenarios, and edge cases. These utilities simplify the creation of test data and help verify that settings are correctly validated, updated, and parsed within a multi-tenant environment.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Tests;
using System;
using System.Collections.Generic;

// Assume 'testInstance' is an instance of TenantSettingsEdgeCaseTests
var testInstance = new TenantSettingsEdgeCaseTests();

// 1. Create a valid TenantSettings instance
var settings = testInstance.CreateValidSettings(settingId: "set-001", tenantId: "tenant-a");

// 2. Create settings with a specific data type
var numericSettings = testInstance.CreateSettingsWithDataType("Int32", "123");

// 3. Create settings with a boolean value
var boolSettings = testInstance.CreateBooleanSettings(true, modifiedBy: "admin");

// 4. Validate settings and get error messages
if (!testInstance.ValidateAndGetErrors(settings, out var errors))
{
    string errorMessages = testInstance.GetValidationErrorMessages(settings);
    Console.WriteLine($"Validation failed: {errorMessages}");
}

// 5. Update a setting and verify the timestamp was updated
var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
testInstance.UpdateAndVerifyTimestamp(settings, "new-value", beforeUpdate);

// 6. Get a value with culture-invariant parsing
int intValue = testInstance.GetValueWithCulture<int>(numericSettings);

// 7. Get a nullable value safely
bool? nullableBool = testInstance.GetNullableValue<bool>(boolSettings);

// 8. Create a collection of settings for batch testing
IReadOnlyList<TenantSettings> settingsCollection = testInstance.CreateSettingsCollection(count: 5);
```

## TenantSizeReportRecord

The `TenantSizeReportRecord` model captures storage metrics for a single tenant SQLite database: its identifier, display name, database path, page-based size figures (`SizeBytes`, `PageCount`, `PageSize`, `FreeListCount`), as well as WAL and on-disk file sizes (`WalSizeBytes`, `FileSizeBytes`). Instances can be ordered with their `CompareTo` method, rendered as rows of a fixed-width text table via `ToTextTableRow`, and combined into tabular or aggregated output through the static `GetTextTableHeader`, `GetTextTableFooter`, and `GetSummaryReport` helpers.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;
using System.Collections.Generic;

// Collect size information for each tenant database
var records = new List<TenantSizeReportRecord>
{
    new TenantSizeReportRecord
    {
        TenantId = "tenant-123",
        TenantName = "Acme Corp",
        DatabasePath = "/data/tenants/tenant-123.db",
        SizeBytes = 1_048_576,
        PageCount = 256,
        PageSize = 4096,
        FreeListCount = 12,
        WalSizeBytes = 65_536,
        FileSizeBytes = 1_114_112
    },
    new TenantSizeReportRecord
    {
        TenantId = "tenant-456",
        TenantName = "Globex Inc",
        DatabasePath = "/data/tenants/tenant-456.db",
        SizeBytes = 2_097_152,
        PageCount = 512,
        PageSize = 4096,
        FreeListCount = 4,
        WalSizeBytes = 131_072,
        FileSizeBytes = 2_228_224
    }
};

// Sort largest-first using the built-in comparison
records.Sort((a, b) => b.CompareTo(a));

// Render a fixed-width text table
Console.WriteLine(TenantSizeReportRecord.GetTextTableHeader());
foreach (TenantSizeReportRecord record in records)
{
    Console.WriteLine(record.ToTextTableRow());
}
Console.WriteLine(TenantSizeReportRecord.GetTextTableFooter());

// Or produce a full summary report across all tenants
string summary = TenantSizeReportRecord.GetSummaryReport(records);
Console.WriteLine(summary);
```


