// existing content ...

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
