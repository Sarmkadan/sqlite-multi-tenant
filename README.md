// existing content ...

## IEncryptionService

The `IEncryptionService` interface provides encryption, decryption, and password hashing capabilities for securing sensitive data in multi-tenant applications. It uses AES-256 encryption with CBC mode and PKCS7 padding, along with PBKDF2 for key derivation. The service automatically handles initialization vectors (IVs) and salts for each encryption operation.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Security;

// Setup in DI container
var services = new ServiceCollection();
services.AddSingleton<IEncryptionService, EncryptionService>();

// Resolve the service
var serviceProvider = services.BuildServiceProvider();
var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();

// Encrypt and decrypt strings
string sensitiveData = "my-secret-password-123";
string encrypted = encryptionService.Encrypt(sensitiveData);
string decrypted = encryptionService.Decrypt(encrypted);

// Encrypt and decrypt byte arrays
byte[] data = System.Text.Encoding.UTF8.GetBytes("sensitive-binary-data");
byte[] encryptedBytes = encryptionService.EncryptBytes(data);
byte[] decryptedBytes = encryptionService.DecryptBytes(encryptedBytes);

// Hash and verify passwords
string password = "user-password-123";
string hashedPassword = encryptionService.HashPassword(password);
bool isValid = encryptionService.VerifyHash(password, hashedPassword);
```

## IRateLimiter

The `IRateLimiter` interface and its implementation `RateLimiter` provide rate limiting functionality to prevent abuse and DoS attacks in multi-tenant applications. It uses a token bucket algorithm with configurable limits per identifier (such as IP address or user ID) and automatically cleans up expired entries.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Security;

// Setup in DI container
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());
services.AddSingleton<IRateLimiter, RateLimiter>();

// Resolve the service
var serviceProvider = services.BuildServiceProvider();
var rateLimiter = serviceProvider.GetRequiredService<IRateLimiter>();

// Check rate limit for an API endpoint
string clientIp = "192.168.1.100";
int maxRequests = 100;
TimeSpan window = TimeSpan.FromMinutes(1);

// First request - should be allowed
var result = await rateLimiter.CheckLimitAsync(clientIp, maxRequests, window);
Console.WriteLine($"Request allowed: {result.IsAllowed}");
Console.WriteLine($"Current count: {result.CurrentCount}/{result.MaxCount}");
Console.WriteLine($"Reset time: {result.ResetTime}");

// Subsequent requests within the window
for (int i = 0; i < 50; i++)
{
    var checkResult = await rateLimiter.CheckLimitAsync(clientIp, maxRequests, window);
    if (!checkResult.IsAllowed)
    {
        Console.WriteLine($"Rate limit exceeded! {checkResult.CurrentCount}/{checkResult.MaxCount}");
        break;
    }
}

// Get current status
var status = await rateLimiter.GetStatusAsync(clientIp);
Console.WriteLine($"Current requests: {status.CurrentCount}");

// Reset the rate limit for a client
await rateLimiter.ResetAsync(clientIp);
```

## EncryptionKeyManager

The `EncryptionKeyManager` class manages encryption keys for multi-tenant applications, providing key generation, rotation, retrieval, and deletion capabilities. It ensures each tenant has its own unique encryption keys and supports key rotation without data loss.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using SqliteMultiTenant.Security;

// Setup in DI container
var services = new ServiceCollection();
services.AddSingleton<EncryptionKeyManager>();

// Resolve the service
var serviceProvider = services.BuildServiceProvider();
var keyManager = serviceProvider.GetRequiredService<EncryptionKeyManager>();

// Generate a new encryption key for a tenant
string tenantId = "acme-corp";
EncryptionKey newKey = await keyManager.GenerateKeyAsync(tenantId);
Console.WriteLine($"Generated key: {newKey.KeyId}, Version: {newKey.Version}");

// Get the active key for a tenant
EncryptionKey activeKey = await keyManager.GetActiveKeyAsync(tenantId);
Console.WriteLine($"Active key: {activeKey.KeyId}, IsActive: {activeKey.IsActive}");

// Rotate the active key for a tenant (creates a new key and marks old one as inactive)
EncryptionKey rotatedKey = await keyManager.RotateKeyAsync(tenantId);
Console.WriteLine($"Rotated key: {rotatedKey.KeyId}, Previous: {rotatedKey.PreviousKeyId}");

// Get a specific key version
EncryptionKey specificKey = await keyManager.GetKeyVersionAsync(tenantId, 1);
Console.WriteLine($"Key version 1: {specificKey.KeyId}");

// Delete all keys for a tenant (when tenant is removed)
bool deleted = await keyManager.DeleteTenantKeysAsync(tenantId);
Console.WriteLine($"Tenant keys deleted: {deleted}");

// EncryptionKey properties
Console.WriteLine($"Key ID: {newKey.KeyId}");
Console.WriteLine($"Tenant ID: {newKey.TenantId}");
Console.WriteLine($"Key Material Length: {newKey.KeyMaterial?.Length ?? 0}");
Console.WriteLine($"Created At: {newKey.CreatedAt}");
Console.WriteLine($"Is Active: {newKey.IsActive}");
Console.WriteLine($"Version: {newKey.Version}");
```

## MigrationException

The `MigrationException` class represents exceptions that occur during database migration operations in multi-tenant applications. It provides detailed information about migration failures including the migration ID, version, and the specific type of failure that occurred. This exception type supports structured error handling for migration-related operations.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Create a migration exception for a failed execution
var executionFailed = new MigrationException("Failed to execute migration 'AddTenantIdColumn' for version '2.1.0'")
{
    MigrationId = "AddTenantIdColumn",
    MigrationVersion = "2.1.0"
};

// Create a rollback failure exception
var rollbackFailed = MigrationException.RollbackFailed("Unable to rollback migration 'CreateTenantsTable' - database in inconsistent state",
    "CreateTenantsTable",
    "1.0.0");

// Create a not found exception
var notFound = MigrationException.NotFound("Migration with ID 'RemoveLegacyColumns' not found in migration history",
    "RemoveLegacyColumns");

// Create an already applied exception
var alreadyApplied = MigrationException.AlreadyApplied("Migration 'AddIndexOnTenantId' has already been applied to database",
    "AddIndexOnTenantId",
    "2.2.0");

// Access exception properties
Console.WriteLine($"Exception Type: {executionFailed.GetType().Name}");
Console.WriteLine($"Message: {executionFailed.Message}");
Console.WriteLine($"Migration ID: {executionFailed.MigrationId}");
Console.WriteLine($"Migration Version: {executionFailed.MigrationVersion}");
```

## TenantNotFoundException

`TenantNotFoundException` is thrown when a requested tenant cannot be found in the system. It derives from `Exception` and exposes the missing tenant identifier through the `TenantId` property, allowing callers to react specifically to tenant‑lookup failures.

### Usage Example

```csharp
using SqliteMultiTenant.Exceptions;

// Service method that looks up a tenant
public Tenant GetTenant(string tenantId)
{
    var tenant = _tenantRepository.FindById(tenantId);
    if (tenant == null)
    {
        // Throw a specific exception that includes the missing tenant ID
        throw new TenantNotFoundException(tenantId);
    }

    return tenant;
}

// Caller handling the exception
try
{
    var tenant = GetTenant("nonexistent-tenant");
}
catch (TenantNotFoundException ex)
{
    Console.WriteLine($"Tenant not found: {ex.TenantId}");
}
```

## StringOperationsBenchmarks

The `StringOperationsBenchmarks` class measures the performance of string operations used in the `SqliteMultiTenant` library. It benchmarks the computation of SHA-256 and MD5 hashes, conversion of strings to snake case and camel case, and sanitization of file paths.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Benchmarks;

var benchmarks = new StringOperationsBenchmarks();

// Compute the SHA-256 hash of a string
string sha256Hash = benchmarks.ComputeSha256Hash("tenant-connection-string:acme-corp:primary-db");

// Compute the MD5 hash of a string
string md5Hash = benchmarks.ComputeMd5Hash("tenant-connection-string:acme-corp:primary-db");

// Convert a string to snake case
string snakeCase = benchmarks.ToSnakeCase("myTenantDatabaseConnectionString");

// Convert a string to camel case
string camelCase = benchmarks.ToCamelCase("my_tenant_database_connection_string");

// Sanitize a file path
string sanitizedFilePath = benchmarks.SanitizeForFilePath("tenant<db>file:name/sub|dir\\test*file");
```

 // existing content ...
