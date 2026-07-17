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