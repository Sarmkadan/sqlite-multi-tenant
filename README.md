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

