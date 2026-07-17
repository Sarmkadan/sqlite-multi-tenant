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