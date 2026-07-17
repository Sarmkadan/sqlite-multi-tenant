## EncryptionKeyManagerExtensions

The `EncryptionKeyManagerExtensions` class provides extension methods for the `EncryptionKeyManager` that simplify tenant-specific key operations with fluent-style APIs. These methods enable easy generation, rotation, retrieval, and management of encryption keys for individual tenants in a multi-tenant SQLite environment.

### Usage Example

```csharp
using SqliteMultiTenant.Security;
using System;

// Assume you have an EncryptionKeyManager instance
var keyManager = new EncryptionKeyManager(...);

// Example 1: Generate a new encryption key for a tenant
var newKey = await keyManager.GenerateKeyForTenantAsync("tenant-123");
Console.WriteLine($"Generated key version: {newKey.Version}");

// Example 2: Generate a key with master password
var secureKey = await keyManager.GenerateKeyForTenantAsync("tenant-123", "MyMasterPassword123!");

// Example 3: Rotate the tenant's encryption key
var rotatedKey = await keyManager.RotateKeyForTenantAsync("tenant-123");
Console.WriteLine($"Rotated to key version: {rotatedKey.Version}");

// Example 4: Check if tenant has an active key
var hasActiveKey = await keyManager.HasActiveKeyAsync("tenant-123");
Console.WriteLine("Tenant has active key: {hasActiveKey}");

// Example 5: Get the active key (returns null if not found)
var activeKey = await keyManager.GetActiveKeyForTenantAsync("tenant-123");

// Example 6: Get the active key (throws if not found)
var requiredKey = await keyManager.GetRequiredActiveKeyForTenantAsync("tenant-123");

// Example 7: Get a specific key version
var historicalKey = await keyManager.GetKeyVersionForTenantAsync("tenant-123", 1);

// Example 8: Delete all keys for a tenant
var deleted = await keyManager.DeleteTenantKeysAsync("tenant-123");
Console.WriteLine($"Keys deleted: {deleted}");
```

## ConnectionManagerIntegrationTests

The `ConnectionManagerIntegrationTests` class provides comprehensive unit tests for the `ConnectionManager` class, verifying that connection management operations work correctly. These tests cover retrieving a connection, reusing an existing connection when available, creating a new connection when the pool is not full, releasing a connection back to the pool, and clearing the tenant pool, ensuring the connection manager operates reliably in multi-tenant SQLite environments.

### Public Members

```csharp
public sealed class ConnectionManagerIntegrationTests : IDisposable
public ConnectionManagerIntegrationTests()
public async Task GetConnectionAsync_ShouldReturnOpenConnection
public async Task GetConnectionAsync_ShouldReuseConnection_WhenAvailable
public async Task GetConnectionAsync_ShouldCreateNewConnection_WhenPoolNotFull
public async Task ReleaseConnectionAsync_ShouldReturnConnectionToPool
public async Task ClearTenantPoolAsync_ShouldRemoveTenantPoolAndDisposeConnections
public async Task GetPoolStatistics_ShouldReturnCorrectStats
public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenTenantIdIsNull
public async Task GetConnectionAsync_ShouldThrowArgumentNullException_WhenConnectionStringIsNull
public async Task GetConnectionAsync_ShouldRespectMaxConnectionsPerPool
public void Dispose
```

### Usage Example

```csharp
using SqliteMultiTenant.Connections;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using System;

// Create logger and checker instance
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConnectionManager>();
var connectionManager = new ConnectionManager(logger);

// Example 1: Get a connection
using var connection = await connectionManager.GetConnectionAsync();
Console.WriteLine(connection != null ? "Connection retrieved" : "Connection retrieval failed");

// Example 2: Release a connection
await connectionManager.ReleaseConnectionAsync(connection);
Console.WriteLine("Connection released");

// Example 3: Get pool statistics
var stats = await connectionManager.GetPoolStatistics();
Console.WriteLine(stats != null ? "Pool statistics retrieved" : "Pool statistics retrieval failed");
```

## HttpClientWrapperValidation

`HttpClientWrapperValidation` is a static helper class that provides validation utilities for `HttpClientWrapper` instances and related HTTP request components such as URLs, bearer tokens, headers, payloads, and response types. It offers methods to retrieve validation problems, check validity, and enforce correctness by throwing exceptions when validation fails.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Integration;

// Assume an existing HttpClientWrapper instance (from the library)
HttpClientWrapper client = new HttpClientWrapper(/* constructor arguments */);

// 1. Validate the wrapper instance and ensure it is correct
IReadOnlyList<string> instanceProblems = HttpClientWrapperValidation.Validate(client);
if (instanceProblems.Count > 0)
{
    Console.WriteLine("HttpClientWrapper has problems:");
    foreach (var p in instanceProblems) Console.WriteLine($"- {p}");
}
else
{
    Console.WriteLine("HttpClientWrapper instance is valid.");
}

// Shortcut to just get a boolean result
bool isValid = HttpClientWrapperValidation.IsValid(client);
Console.WriteLine($"IsValid: {isValid}");

// Throw an exception if the instance is not valid
HttpClientWrapperValidation.EnsureValid(client);

// 2. Validate a request URL
var urlProblems = HttpClientWrapperValidation.ValidateUrl("https://api.example.com/v1/resource");
Console.WriteLine(urlProblems.Count == 0 ? "URL is valid." : $"URL problems: {string.Join(", ", urlProblems)}");

// 3. Validate a bearer token
var tokenProblems = HttpClientWrapperValidation.ValidateBearerToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
Console.WriteLine(tokenProblems.Count == 0 ? "Bearer token is valid." : $"Token problems: {string.Join(", ", tokenProblems)}");

// 4. Validate a custom header
var headerProblems = HttpClientWrapperValidation.ValidateHeader("X-Custom-Header", "HeaderValue");
Console.WriteLine(headerProblems.Count == 0 ? "Header is valid." : $"Header problems: {string.Join(", ", headerProblems)}");

// 5. Validate a payload object before serialization
var payload = new { Id = 123, Name = "Sample" };
var payloadProblems = HttpClientWrapperValidation.ValidatePayload(payload);
Console.WriteLine(payloadProblems.Count == 0 ? "Payload is valid." : $"Payload problems: {string.Join(", ", payloadProblems)}");

// 6. Validate a response type for deserialization
public class ApiResponse
{
    public int Code { get; set; }
    public string? Message { get; set; }

    // Parameterless constructor required by the validator
    public ApiResponse() { }
}

var responseTypeProblems = HttpClientWrapperValidation.ValidateResponseType<ApiResponse>();
Console.WriteLine(responseTypeProblems.Count == 0 ? "Response type is valid." : $"Response type problems: {string.Join(", ", responseTypeProblems)}");
```
