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


## RateLimitingMiddlewareValidation

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

/* 
 * Example 1: Verify connection-per-tenant isolation
 */
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

/* 
 * Example 2: Verify shared-schema isolation
 */
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
```


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
```


## TenantIntegrityCheckResult

The `TenantIntegrityCheckResult` model captures the outcome of an SQLite `PRAGMA integrity_check` executed against a single tenant database. It identifies the tenant via `TenantId` and `TenantName`, reports whether the database passed the check through `IsOk`, and exposes diagnostics via `Error` and the raw SQLite output in `IntegrityOutput`. Every result is stamped with `CheckedAt`, so integrity history can be tracked over time.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Example 1: Record a successful integrity check
var okResult = new TenantIntegrityCheckResult
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    IsOk = true,
    Error = null,
    IntegrityOutput = "ok",
    CheckedAt = DateTime.UtcNow
};

// Example 2: Record a failed check with SQLite's diagnostic output
var failedResult = new TenantIntegrityCheckResult
{
    TenantId = "tenant-456",
    TenantName = "Globex Inc",
    IsOk = false,
    Error = null,
    IntegrityOutput = "*** in database main ***\nPage 3: never used",
    CheckedAt = DateTime.UtcNow
};

// Report the outcomes
Console.WriteLine($"{okResult.TenantName} ({okResult.TenantId}): {(okResult.IsOk ? "OK" : "FAILED")} checked at {okResult.CheckedAt:u}");

if (!failedResult.IsOk)
{
    Console.WriteLine($"Integrity problems detected for {failedResult.TenantName} ({failedResult.TenantId}):");
    Console.WriteLine(failedResult.IntegrityOutput);
}
```
```


## TenantDatabaseMaintenanceService

The `TenantDatabaseMaintenanceService` performs routine SQLite maintenance operations—`VACUUM`, `ANALYZE`, and `PRAGMA optimize`—against individual tenant databases or across every tenant in the system. Single-tenant operations return a `TenantMaintenanceResult` describing the outcome for that database, while the batch variants run the same operation for all tenants and return one result per tenant. Use it to reclaim disk space, refresh query planner statistics, and keep long-lived multi-tenant databases healthy.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Assume 'service' is an instance of TenantDatabaseMaintenanceService
// Assume 'cancellationToken' is available

// 1. Reclaim unused space in a single tenant database
TenantMaintenanceResult vacuumResult = await service.VacuumTenantDatabaseAsync("tenant-123", cancellationToken);

// 2. Vacuum every tenant database
List<TenantMaintenanceResult> vacuumResults = await service.VacuumAllTenantDatabasesAsync(cancellationToken);
Console.WriteLine($"Vacuumed {vacuumResults.Count} tenant databases.");

// 3. Refresh query planner statistics for a single tenant database
TenantMaintenanceResult analyzeResult = await service.AnalyzeTenantDatabaseAsync("tenant-456", cancellationToken);

// 4. Analyze every tenant database
List<TenantMaintenanceResult> analyzeResults = await service.AnalyzeAllTenantDatabasesAsync(cancellationToken);

// 5. Run incremental optimization for a single tenant database
TenantMaintenanceResult optimizeResult = await service.OptimizeTenantDatabaseAsync("tenant-789", cancellationToken);

// 6. Optimize every tenant database
List<TenantMaintenanceResult> optimizeResults = await service.OptimizeAllTenantDatabasesAsync(cancellationToken);

// 7. Run the complete maintenance pipeline (vacuum, analyze, optimize) for a single tenant
TenantMaintenanceResult fullResult = await service.PerformFullMaintenanceAsync("tenant-123", cancellationToken);

// 8. Run the complete maintenance pipeline across all tenants
List<TenantMaintenanceResult> fullResults = await service.PerformFullMaintenanceOnAllAsync(cancellationToken);
Console.WriteLine($"Completed full maintenance on {fullResults.Count} tenant databases.");
```
```


## TenantSizeReportService

The `TenantSizeReportService` generates storage size reports for tenant SQLite databases. It collects page-based size figures (`SizeBytes`, `PageCount`, `PageSize`, `FreeListCount`) together with WAL and on-disk file sizes into a `TenantSizeReportRecord` per tenant, either for a single tenant or across all tenants in the system. Results can be rendered as a fixed-width text table via `GenerateTextTableReportAsync`, or combined into a full human-readable report with `GenerateCompleteReportAsync`.

### Usage Example

```csharp
using SqliteMultiTenant.Services;
using SqliteMultiTenant.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Assume 'service' is an instance of TenantSizeReportService
// Assume 'cancellationToken' is available

// 1. Generate a size report for a single tenant database
TenantSizeReportRecord record = await service.GenerateReportForTenantAsync("tenant-123", cancellationToken);
Console.WriteLine($"{record.TenantName}: {record.SizeBytes} bytes ({record.PageCount} pages)");

// 2. Generate size reports for every tenant database
List<TenantSizeReportRecord> records = await service.GenerateReportForAllTenantsAsync(cancellationToken);
records.Sort((a, b) => b.CompareTo(a));

// 3. Render the collected records as a fixed-width text table
string table = await service.GenerateTextTableReportAsync(cancellationToken);
Console.WriteLine(table);

// 4. Produce a complete report combining per-tenant details and summary totals
string completeReport = await service.GenerateCompleteReportAsync(cancellationToken);
Console.WriteLine(completeReport);
```
```


## BackupVerificationResult

The `BackupVerificationResult` model captures the outcome of a SQLite backup file integrity verification. It reports whether the backup passed the check via `IsValid`, exposes the raw SQLite integrity check message through `IntegrityCheckResult`, and records the file's physical characteristics (`FileSizeBytes`, `PageCount`, `PageSizeBytes`) together with the `VerifiedAt` timestamp. Failed verifications carry a diagnostic `ErrorMessage`, and the static `Success` and `Failed` factory methods make it easy to construct either outcome.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Example 1: Build a successful verification result
var okResult = BackupVerificationResult.Success(
    integrityResult: "ok",
    fileSize: 1_114_112,
    pageCount: 272,
    pageSize: 4096);
Console.WriteLine($"Valid: {okResult.IsValid}, Size: {okResult.FileSizeBytes} bytes, Verified at: {okResult.VerifiedAt:u}");

// Example 2: Build a failed verification result
var failedResult = BackupVerificationResult.Failed("Backup file is missing or corrupted");
if (!failedResult.IsValid)
{
    Console.WriteLine($"Verification failed: {failedResult.ErrorMessage}");
}

// Example 3: Inspect a result's integrity details
if (okResult.IsValid)
{
    Console.WriteLine($"Integrity: {okResult.IntegrityCheckResult}");
    Console.WriteLine($"Pages: {okResult.PageCount} x {okResult.PageSizeBytes} bytes");
}
```
```


## TenantMaintenanceResult

The `TenantMaintenanceResult` model captures the outcome of a single tenant database maintenance operation. It identifies the tenant via `TenantId` and `TenantName`, records which `Operation` was performed (`VACUUM`, `ANALYZE`, etc.), and tracks timing through `StartedAt` and `CompletedAt`. It also reports the database file size before and after the operation (`SizeBeforeBytes`, `SizeAfterBytes`, and the optional `IntermediateSizeBytes`), with `Error` carrying a diagnostic message when the operation fails.

### Usage Example

```csharp
using SqliteMultiTenant.Models;
using System;

// Example 1: Record a successful VACUUM operation
var vacuumResult = new TenantMaintenanceResult
{
    TenantId = "tenant-123",
    TenantName = "Acme Corp",
    Operation = "VACUUM",
    StartedAt = DateTime.UtcNow.AddSeconds(-3),
    CompletedAt = DateTime.UtcNow,
    SizeBeforeBytes = 1_048_576,
    SizeAfterBytes = 524_288,
    IntermediateSizeBytes = 524_288,
    Error = null
};

// Example 2: Record a failed ANALYZE operation
var analyzeResult = new TenantMaintenanceResult
{
    TenantId = "tenant-456",
    TenantName = "Globex Inc",
    Operation = "ANALYZE",
    StartedAt = DateTime.UtcNow,
    CompletedAt = null,
    SizeBeforeBytes = 2_097_152,
    SizeAfterBytes = 2_097_152,
    IntermediateSizeBytes = null,
    Error = "Database is locked"
};

// Report the outcomes
Console.WriteLine($"{vacuumResult.TenantName} ({vacuumResult.TenantId}): {vacuumResult.Operation} started at {vacuumResult.StartedAt:u}");

if (vacuumResult.CompletedAt.HasValue)
{
    Console.WriteLine($"Completed at {vacuumResult.CompletedAt:u}; size {vacuumResult.SizeBeforeBytes} -> {vacuumResult.SizeAfterBytes} bytes");
}

if (analyzeResult.Error != null)
{
    Console.WriteLine($"Operation failed for {analyzeResult.TenantName} ({analyzeResult.TenantId}): {analyzeResult.Error}");
}
```
```


## ValidationRuleBuilderTests

The `ValidationRuleBuilderTests` class contains unit tests for the `ValidationRuleBuilder<T>` class, which provides a fluent interface for building validation rules for model properties. It supports validation rules such as Required, Email, StringLength, Range, Pattern, Custom, and MustMatch, allowing developers to compose complex validation logic in a readable, chainable manner.

### Usage Example

```csharp
using SqliteMultiTenant.Validation;
using System;

// Example model to validate
public class UserModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
}

// Example usage of ValidationRuleBuilder
var user = new UserModel 
{ 
    Name = "John Doe", 
    Email = "john@example.com", 
    Age = 30,
    Password = "SecurePass123!",
    ConfirmPassword = "SecurePass123!",
    Phone = "123-456-7890",
    Title = "Software Engineer",
    Price = 29.99m,
    Stock = 100
};

var builder = new ValidationRuleBuilder<UserModel>();
builder.Required("Name")
       .Required("Email")
       .Email("Email")
       .StringLength("Name", minLength: 2, maxLength: 50)
       .Range("Age", minValue: 18, maxValue: 100)
       .StringLength("Password", minLength: 8)
       .MustMatch("Password", "ConfirmPassword")
       .Pattern("Phone", @"^\d{3}-\d{3}-\d{4}$")
       .Range("Price", minValue: 0.01m, maxValue: 1000m)
       .Range("Stock", minValue: 0);

var result = builder.Validate(user);

if (result.IsValid)
{
    Console.WriteLine("Validation passed!");
}
else
{
    Console.WriteLine("Validation failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error.FieldName}: {error.Message}");
    }
}
```
```


## TenantDatabaseMaintenanceServiceExtensions

The `TenantDatabaseMaintenanceServiceExtensions` class provides extension methods for registering the `TenantDatabaseMaintenanceService` with the .NET dependency injection container. It offers two overloads of `AddTenantDatabaseMaintenanceService`: one for simple registration with default options, and one that accepts an `Action<TenantDatabaseMaintenanceOptions>` to customize maintenance behavior such as which operations are enabled, the interval between runs, per-operation timeouts, and the degree of parallelism.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Services;

// In Program.cs or Startup.cs
var services = new ServiceCollection();

// 1. Register with default options (vacuum, analyze, and optimize enabled)
services.AddTenantDatabaseMaintenanceService();

// 2. Register with custom configuration
services.AddTenantDatabaseMaintenanceService(options =>
{
    options.EnableVacuum = true;
    options.EnableAnalyze = true;
    options.EnableOptimize = false;
    options.IntervalHours = 12;          // Run maintenance every 12 hours
    options.TimeoutSeconds = 120;        // 2 minutes per operation
    options.DegreeOfParallelism = 2;     // Process two tenants in parallel
});
```
```


## SqlCipherConnectionBuilderTests

The `SqlCipherConnectionBuilderTests` class contains unit tests for the `SqlCipherConnectionBuilder` class, covering connection string building, encryption key application, and rekeying functionality.

### Usage Example

```csharp
using SqliteMultiTenant.Security;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

// Example usage of SqlCipherConnectionBuilder
var dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".db");
var key = "my-secret-key";

// Build a connection string
var connectionString = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, key);

// Apply encryption key to a connection
await using var connection = new SQLiteConnection(connectionString);
await connection.OpenAsync();
await SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(connection, key);
await connection.CloseAsync();

// Rekey the database
await using var connection2 = new SQLiteConnection(connectionString);
await connection2.OpenAsync();
await SqlCipherConnectionBuilder.RekeyAsync(connection2, "new-key");
await connection2.CloseAsync();

// Clean up
File.Delete(dbPath);
```
```


## EventPublisherTests

The `EventPublisherTests` class contains unit tests for the `EventPublisher` class, validating the publish-subscribe pattern implementation, handler registration mechanisms, and error handling scenarios. These tests demonstrate how to properly use the event publishing system in a multi-tenant SQLite environment.

### Usage Example

```csharp
using SqliteMultiTenant.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

// Create an event publisher (typically via dependency injection)
var logger = new Logger<EventPublisher>(new LoggerFactory());
var publisher = new EventPublisher(logger);

// Define a custom event
public class UserCreatedEvent : DomainEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    
    public UserCreatedEvent() : base(nameof(UserCreatedEvent)) { }
}

// Define an event handler
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public bool WasCalled { get; private set; }
    public UserCreatedEvent? LastEvent { get; private set; }
    
    public Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastEvent = @event;
        // Process the user creation (e.g., send welcome email)
        return Task.CompletedTask;
    }
}

// Subscribe to the event
var handler = new UserCreatedEventHandler();
publisher.Subscribe(handler);

// Publish an event
var userCreatedEvent = new UserCreatedEvent
{
    UserId = "user-123",
    Email = "user@example.com"
};

await publisher.PublishAsync(userCreatedEvent);

// Check handler invocation count
int handlerCount = publisher.GetHandlerCount<UserCreatedEvent>();
// handlerCount should be 1
```
```


## BackupExceptionJsonExtensionsTests

The `BackupExceptionJsonExtensionsTests` class contains unit tests for the JSON serialization and deserialization extension methods on the `BackupException` class. It verifies that `BackupException` instances can be serialized to JSON strings and handles edge cases such as null inputs and invalid JSON.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Extensions; // Assuming the extension methods are in this namespace
using System;

// Example: Serializing a BackupException to JSON
var exception = new BackupException("Backup failed", "backup-123", "db-456");
string json = exception.ToJson();
Console.WriteLine($"Serialized JSON: {json}");

// Example: Serializing with indentation
string indentedJson = exception.ToJson(indented: true);
Console.WriteLine($"Indented JSON:\n{indentedJson}");

// Example: Deserialization throws NotSupportedException because BackupException lacks a parameterless constructor
try
{
    var restored = BackupExceptionJsonExtensions.FromJson(json);
}
catch (NotSupportedException)
{
    Console.WriteLine("Deserialization not supported due to missing parameterless constructor in BackupException.");
}

// Example: Using TryFromJson also throws NotSupportedException
try
{
    BackupExceptionJsonExtensions.TryFromJson(json, out var restored);
}
catch (NotSupportedException)
{
    Console.WriteLine("TryFromJson also throws NotSupportedException for the same reason.");
}
```
```


## StringUtilitiesTests

The `StringUtilitiesTests` class contains unit tests for the `StringUtilities` class, covering string manipulation and validation methods including hashing, truncation, case conversion, whitespace removal, sanitization, validation, and generation utilities.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;

// Example: Using various string utilities for data processing
string userInput = "  Hello World!  ";
string fileName = "my file:test*.txt";
string email = "user@example.com";
string url = "https://example.com/path";

// 1. Remove whitespace
string trimmed = StringUtilities.RemoveWhitespace(userInput); // "HelloWorld!"

// 2. Sanitize for file paths
string safeFileName = StringUtilities.SanitizeForFilePath(fileName); // "my_file_test_.txt"

// 3. Validate email and URL
bool isValidEmail = StringUtilities.IsValidEmail(email); // true
bool isValidUrl = StringUtilities.IsValidUrl(url); // true

// 4. Convert to different cases
string snakeCase = StringUtilities.ToSnakeCase("helloWorld"); // "hello_world"
string camelCase = StringUtilities.ToCamelCase("hello_world"); // "helloWorld"
string titleCase = StringUtilities.ToTitleCase("hello world"); // "Hello World"

// 5. Generate hashes
string sha256Hash = StringUtilities.ComputeSha256Hash("test"); // Non-empty 64-char hex string
string md5Hash = StringUtilities.ComputeMd5Hash("test"); // Non-empty 32-char hex string

// 6. Truncate with ellipsis
string truncated = StringUtilities.TruncateWithEllipsis("This is a very long string", 10); // "This is..."

// 7. Generate random string
string random = StringUtilities.GenerateRandomString(8); // 8-character alphanumeric string
```
```


## PathUtilitiesTests

The `PathUtilitiesTests` class contains unit tests for the `PathUtilities` class, covering file system path manipulation utilities including safe path combination, directory creation/deletion, file enumeration, path normalization, and byte formatting.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using System;
using System.IO;

// Example: Using path utilities for safe file system operations
string basePath = "/home/user/documents";
string relativePath = "projects/project1";

// 1. Safely combine paths (prevents path traversal)
string combinedPath = PathUtilities.SafeCombinePath(basePath, relativePath);
// Returns: "/home/user/documents/projects/project1"

// 2. Safely create a directory
bool created = PathUtilities.SafeCreateDirectory(combinedPath);
// Returns true if directory was created or already exists

// 3. Get size of a directory
long size = PathUtilities.GetDirectorySizeBytes(combinedPath);
// Returns total size in bytes of all files in directory

// 4. Get files recursively with filter
var txtFiles = PathUtilities.GetFilesRecursive(combinedPath, "*.txt");
// Returns list of .txt files in directory and subdirectories

// 5. Normalize path separators
string normalized = PathUtilities.NormalizePath("folder\\subfolder/file.txt");
// Returns platform-specific path with correct separators

// 6. Make a path relative
string relative = PathUtilities.MakeRelativePath("/home/user", "/home/user/documents/file.txt");
// Returns "documents/file.txt"

// 7. Format byte count for display
string formatted = PathUtilities.FormatBytes(1500);
// Returns "1.46 KB"
```
```


## CacheStrategyTests

The `CacheStrategyTests` class contains unit tests for the `LruCacheStrategy` and `TimeBasedCacheStrategy` classes, covering public policy decisions (eviction/expiry selection) with boundary values.

### Usage Example

Within the `CacheStrategyTests` class, the following test method verifies the LRU eviction policy:

```csharp
[Fact]
public async Task LruCacheStrategy_EvictsLeastRecentlyUsed_WhenCacheIsFull()
{
    // Arrange
    const int maxSize = 2;
    _lruLoggerMock.LogInformation("Starting LruCacheStrategy eviction test with max size {MaxSize}", maxSize);
    var cache = new LruCacheStrategy(_lruLoggerMock, maxSize);

    // Fill cache to capacity
    await cache.SetAsync("key1", "value1");
    await cache.SetAsync("key2", "value2");

    // Access key1 to make it recently used
    await cache.GetAsync<string>("key1");

    // Add third item - should evict key2 (least recently used)
    await cache.SetAsync("key3", "value3");

    // Assert
    var value1 = await cache.GetAsync<string>("key1");
    var value2 = await cache.GetAsync<string>("key2");
    var value3 = await cache.GetAsync<string>("key3");

    value1.Should().Be("value1"); // Should still exist (recently used)
    value2.Should().BeNull();     // Should be evicted (LRU)
    value3.Should().Be("value3"); // Should exist (just added)

    _lruLoggerMock.LogInformation("Completed LruCacheStrategy eviction test; key1={Key1}, key2={Key2}, key3={Key3}", value1, value2, value3);
}
```
```


## EncryptionServiceTests

The `EncryptionServiceTests` class contains unit tests for the `EncryptionService` class.
These tests cover encryption and decryption of strings and bytes, password hashing and verification, and various edge cases including empty/null inputs, invalid data, and wrong keys. These tests validate that the encryption service correctly handles both string and byte data, properly manages empty and null inputs, and throws appropriate exceptions for invalid inputs or incorrect keys.

### Usage Example

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Security;

// Setup configuration with encryption key (must be 32 characters for AES-256)
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Encryption:Key"] = "my-encryption-key-32-chars-long!!"
    })
    .Build();

// Create logger (using null logger for simplicity)
ILogger<EncryptionService> logger = NullLogger<EncryptionService>.Instance;

// Create encryption service
var encryptionService = new EncryptionService(config, logger);

// Encrypt a string
string plainText = "Hello, World!";
string cipherText = encryptionService.Encrypt(plainText);
Console.WriteLine($"Encrypted: {cipherText}");

// Decrypt the string
string decryptedText = encryptionService.Decrypt(cipherText);
Console.WriteLine($"Decrypted: {decryptedText}");

// Hash a password
string password = "MySecurePassword123!";
string hash = encryptionService.HashPassword(password);
Console.WriteLine($"Hash: {hash}");

// Verify the hash
bool isValid = encryptionService.VerifyHash(password, hash);
Console.WriteLine($"Password valid: {isValid}");
```
```


## DataMapperTests

The `DataMapperTests` class contains unit tests for the `DataMapper` class, verifying its ability to map properties between source and target objects, including handling nulls, case insensitivity, and collections.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using System;

// Define simple source and target types for demonstration
public class Source
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double? Value { get; set; }
}

public class Target
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Value { get; set; }
}

// Example usage
var source = new Source { Id = 1, Name = "Test", Value = 42.5 };
var mapper = new DataMapper(NullLogger<DataMapper>.Instance);
var target = mapper.Map<Source, Target>(source);

// target.Id should be 1
// target.Name should be "Test"
// target.Value should be 42.5
```

## OperationRetryPolicyTests

The `OperationRetryPolicyTests` class contains unit tests for the `OperationRetryPolicy` class, verifying retry behavior for transient and non-transient exceptions, custom configurations, and edge cases such as null operations and exhausted retries.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Utilities;
using System;
using System.Threading.Tasks;

// Create a logger substitute and retry policy with 3 retries, 10ms initial delay, 2.0 backoff multiplier
var logger = Substitute.For<ILogger<OperationRetryPolicy>>();
var retryPolicy = new OperationRetryPolicy(logger, maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);

// Define an operation that fails twice then succeeds
int attempt = 0;
Task<string> operation()
{
    attempt++;
    if (attempt < 3)
    {
        throw new TimeoutException("Simulated timeout");
    }
    return Task.FromResult("Success");
}

// Execute the operation with retry policy
string result = await retryPolicy.ExecuteAsync(operation, "TestOperation");

// Result should be "Success" and attempt should be 3 (1 initial + 2 retries)
```

## BatchProcessorConcurrencyTests

The `BatchProcessorConcurrencyTests` class verifies that batch processing preserves every item across sequential, concurrent, and very high-concurrency workloads. It also checks that empty inputs and result-free operations are handled correctly, while single, multiple, and all-item failures remain isolated and produce accurate statistics.

### Usage Example

The methods are normally discovered and run by xUnit, but they can also be invoked directly from asynchronous test harness code:

```csharp
using SqliteMultiTenant.Tests;

var tests = new BatchProcessorConcurrencyTests();

await tests.ProcessAsync_WithManyItems_ShouldPreserveAllItems();
await tests.ProcessAsync_WithExceptionInOneBatch_ShouldNotCorruptOtherBatches();
await tests.ProcessAsync_WithEmptyCollection_ShouldReturnEmptyResult();
```

## BackupRotationManagerTests

The `BackupRotationManagerTests` class verifies that backup rotation enforces count and age limits, including the cutoff-date boundary and same-day backups. It also covers empty or missing directories, an exact count limit, combined retention rules, and zero-valued policy settings. The methods are xUnit tests and can be discovered by a test runner or invoked directly from asynchronous test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests.BackgroundWorkers;

var tests = new BackupRotationManagerTests();

await tests.RotateBackupsAsync_ShouldKeepExactlyMaxBackupCount_WhenExceedingCount();
await tests.RotateBackupsAsync_ShouldDeleteBackupsOlderThanMaxAge();
await tests.RotateBackupsAsync_ShouldEnforceBothAgeAndCountLimits();
await tests.RotateBackupsAsync_ShouldHandleNonExistentDirectory();
```

## BulkInsertBuilderTests

The `BulkInsertBuilderTests` class is an xUnit test fixture that verifies `BulkInsertBuilder` constructor validation, null argument handling, fluent record addition, and SQL generation for empty, single, and multiple records. It also checks how generated SQL handles null values and escapes special characters; the test methods are normally discovered by a test runner but can be invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests.Operations;

var tests = new BulkInsertBuilderTests();

tests.BulkInsertBuilder_GenerateSqlStatements_WithEmptyRecords_ReturnsEmptyString();
tests.BulkInsertBuilder_GenerateSqlStatements_WithSingleRecord_GeneratesCorrectSql();
tests.BulkInsertBuilder_GenerateSqlStatements_WithMultipleRecords_GeneratesCorrectSql();
tests.BulkInsertBuilder_GenerateSqlStatements_WithNullValues_HandlesCorrectly();
tests.BulkInsertBuilder_GenerateSqlStatements_WithSpecialCharactersInValues_EscapesCorrectly();
tests.BulkInsertBuilder_AddRecord_ReturnsBuilderForFluentInterface();
tests.BulkInsertBuilder_AddRecords_ReturnsBuilderForFluentInterface();
```

## MigrationExceptionExtensionsTests

The `MigrationExceptionExtensionsTests` class is an xUnit test fixture that verifies migration exceptions can be classified as execution failures or already-applied versions regardless of message casing. It also checks migration-detail formatting for populated, null, and empty identifiers and versions, along with null-exception argument validation; the public test methods are normally discovered by xUnit but can also be invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests;

var tests = new MigrationExceptionExtensionsTests();

tests.IsExecutionFailure_WithExecutionFailedMessage_ShouldReturnTrue();
tests.IsExecutionFailure_WithNonExecutionFailedMessage_ShouldReturnFalse();
tests.IsVersionAlreadyApplied_WithAlreadyAppliedMessage_ShouldReturnTrue();
tests.IsVersionAlreadyApplied_WithNonAlreadyAppliedMessage_ShouldReturnFalse();
tests.GetMigrationDetails_WithNonNullMigrationIdAndVersion_ShouldReturnFormattedString();
tests.GetMigrationDetails_WithBothNullMigrationIdAndVersion_ShouldReturnNullForBoth();
tests.GetMigrationDetails_FromExecutionFailedException_ShouldReturnCorrectDetails();
```

## MigrationExceptionTests

The `MigrationExceptionTests` class is an xUnit test fixture that verifies `MigrationException` constructors preserve messages, migration identifiers, versions, and inner exceptions. It also covers the `ExecutionFailed`, `RollbackFailed`, `NotFound`, and `AlreadyApplied` factory methods, including empty identifiers, empty versions, and null values; its public test methods are normally discovered by xUnit but can also be invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests;

var tests = new MigrationExceptionTests();

tests.Constructor_WithMessage_ShouldSetMessage();
tests.Constructor_WithMessageAndInnerException_ShouldSetProperties();
tests.Constructor_WithAllParameters_ShouldSetAllProperties();
tests.Constructor_WithNullVersion_ShouldHandleNullVersion();
tests.ExecutionFailed_ShouldCreateProperException();
tests.ExecutionFailed_WithEmptyMigrationId_ShouldCreateException();
tests.RollbackFailed_ShouldCreateProperException();
tests.RollbackFailed_WithNullInnerException_ShouldCreateException();
tests.NotFound_ShouldCreateProperException();
tests.NotFound_WithEmptyMigrationId_ShouldCreateException();
tests.AlreadyApplied_ShouldCreateProperException();
tests.AlreadyApplied_WithEmptyVersion_ShouldCreateException();
```

## InsertUpdateBuilderTests

The `InsertUpdateBuilderTests` class is an xUnit test fixture that verifies insert and update builders generate the expected SQL and parameter collections for single, multiple, and null values. It also checks identifier quoting, validates table, column, and `WHERE` inputs, and ensures incomplete statements cannot be built; the methods are normally discovered by xUnit but can also be invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests.DataOperations;

var tests = new InsertUpdateBuilderTests();

tests.InsertBuilder_SingleValue_BuildsCorrectQueryAndParameters();
tests.InsertBuilder_MultipleValues_BuildsCorrectQueryAndParameters();
tests.InsertBuilder_ValueWithNull_StoresAsDBNull();
tests.InsertBuilder_SpecialColumnNames_QuotesColumnsCorrectly();
tests.UpdateBuilder_SingleSetValue_BuildsCorrectQueryAndParameters();
tests.UpdateBuilder_MultipleSetValues_BuildsCorrectQueryAndParameters();
tests.UpdateBuilder_SetWithNull_StoresAsDBNull();
tests.UpdateBuilder_NoWhereClause_ThrowsInvalidOperationException();
```

## RateLimiterTests

The `RateLimiterTests` class is an xUnit test fixture that verifies requests are allowed or rejected according to their configured limits, expired windows are cleared, and identifiers maintain independent counts. It also covers reset and status behavior, reset-time calculations, statistics, null-logger initialization, removal of old requests, and thread safety under rapid concurrent requests; the asynchronous test methods can be discovered by xUnit or invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests;

await new RateLimiterTests().CheckLimitAsync_WithRequestsUnderLimit_ShouldAllow();
await new RateLimiterTests().CheckLimitAsync_WithRequestsOverLimit_ShouldReject();
await new RateLimiterTests().CheckLimitAsync_AfterWindowExpires_ShouldResetCount();
await new RateLimiterTests().CheckLimitAsync_WithDifferentIdentifiers_ShouldMaintainIndependentLimits();
await new RateLimiterTests().ResetAsync_ShouldClearRateLimit();
await new RateLimiterTests().GetStatusAsync_ForExistingIdentifier_ShouldReturnCorrectStatus();
await new RateLimiterTests().GetStatisticsAsync_ShouldReturnValidStatistics();
await new RateLimiterTests().CheckLimitAsync_MultipleRapidRequests_ShouldBeThreadSafe();

new RateLimiterTests().Service_Initialization_WithNullLogger_ShouldNotThrow();
```

## TenantQuotaEnforcerTests

The `TenantQuotaEnforcerTests` class is an xUnit test fixture that verifies tenant quota checks for under-quota, boundary, over-quota, unlimited, near-quota, and missing-tenant scenarios. It also covers automatic suspension during enforcement, quota metadata validation and retrieval, and sorted scanning of tenants near or over their quotas; its public asynchronous test methods can be discovered by xUnit or invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests.Tenants;

var tests = new TenantQuotaEnforcerTests();

await tests.CheckQuotaAsync_UnderQuotaAllowed_ReturnsCorrectResult();
await tests.CheckQuotaAsync_AtBoundaryQuota_ReturnsOverQuota();
await tests.CheckQuotaAsync_NearQuotaWarning_ReturnsNearQuotaTrue();
await tests.EnforceAsync_OverQuotaWithAutoSuspend_CallsSuspendTenant();
await tests.SetQuotaAsync_PositiveMaxBytes_SetsMetadataCorrectly();
await tests.GetQuotaAsync_WithValidQuotaMetadata_ReturnsParsedValue();
await tests.ScanAllAsync_ReturnsTenantsNearOrOverQuota_SortedByUsage();
```

## BackupExceptionExtensionsTests

The `BackupExceptionExtensionsTests` class is an xUnit test fixture that verifies backup exceptions are classified as creation, verification, or restore failures using case-insensitive message matching. It also covers empty messages, null-exception validation, and error-detail formatting for populated, null, and empty backup and database identifiers; its public test methods can be discovered by xUnit or invoked directly from test harness code.

### Usage Example

```csharp
using SqliteMultiTenant.Tests;

var tests = new BackupExceptionExtensionsTests();

tests.IsCreationFailure_WithCreationMessage_ShouldReturnTrue();
tests.IsCreationFailure_WithNonCreationMessage_ShouldReturnFalse();
tests.IsCreationFailure_WithCaseInsensitiveCreationMessage_ShouldReturnTrue();
tests.IsCreationFailure_WithEmptyMessage_ShouldReturnFalse();

tests.IsVerificationFailure_WithVerificationMessage_ShouldReturnTrue();
tests.IsVerificationFailure_WithNonVerificationMessage_ShouldReturnFalse();
tests.IsVerificationFailure_WithCaseInsensitiveVerificationMessage_ShouldReturnTrue();
tests.IsVerificationFailure_WithEmptyMessage_ShouldReturnFalse();

tests.IsRestoreFailure_WithRestoreMessage_ShouldReturnTrue();
tests.IsRestoreFailure_WithNonRestoreMessage_ShouldReturnFalse();
tests.IsRestoreFailure_WithCaseInsensitiveRestoreMessage_ShouldReturnTrue();
tests.IsRestoreFailure_WithEmptyMessage_ShouldReturnFalse();

tests.GetErrorDetails_WithValidException_ShouldReturnFormattedString();
tests.GetErrorDetails_WithNullBackupId_ShouldIncludeNullInOutput();
tests.GetErrorDetails_WithNullDatabaseId_ShouldIncludeNullInOutput();
tests.GetErrorDetails_WithEmptyStrings_ShouldReturnFormattedString();
```
