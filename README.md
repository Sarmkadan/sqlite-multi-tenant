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
