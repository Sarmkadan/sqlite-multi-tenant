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