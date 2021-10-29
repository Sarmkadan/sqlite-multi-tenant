# ValidationExtensions

The `ValidationExtensions` static class provides a centralized set of utility methods for validating common data formats and configuration values within the `sqlite-multi-tenant` application. These utilities ensure consistent data integrity and security checks across the system by providing standardized validation logic for identifiers, network settings, and database-related configurations.

## API

### IsValidEmail
`public static bool IsValidEmail(string? email)`
Validates whether the provided string conforms to a standard email address format.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidUuid
`public static bool IsValidUuid(string? uuid)`
Validates whether the provided string is a valid UUID/GUID format.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidSemanticVersion
`public static bool IsValidSemanticVersion(string? version)`
Validates whether the provided string conforms to the Semantic Versioning (SemVer) format (e.g., "1.2.3").
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidDatabaseName
`public static bool IsValidDatabaseName(string? dbName)`
Validates whether the provided string is a compliant database name identifier.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidTenantName
`public static bool IsValidTenantName(string? tenantName)`
Validates whether the provided string adheres to the naming conventions for a tenant.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidRelativePath
`public static bool IsValidRelativePath(string? path)`
Validates whether the provided string represents a safe and valid relative filesystem path.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidSqlScript
`public static bool IsValidSqlScript(string? sql)`
Validates whether the provided string contains a basic, structurally valid SQL script.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidPort
`public static bool IsValidPort(string? port)`
Validates whether the provided string represents a valid TCP/IP port number (1-65535).
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidConnectionString
`public static bool IsValidConnectionString(string? connectionString)`
Validates whether the provided string is a structurally valid connection string.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidBackupTag
`public static bool IsValidBackupTag(string? tag)`
Validates whether the provided string adheres to the format required for a backup tag.
- **Returns**: `true` if valid; otherwise, `false`.

### IsNullOrEmpty<T>
`public static bool IsNullOrEmpty<T>(T value)`
Checks if the provided value is null or, for supported types, if it is empty.
- **Returns**: `true` if the value is null or empty; otherwise, `false`.

### IsValidRetentionDays
`public static bool IsValidRetentionDays(string? days)`
Validates whether the provided string represents a valid number of days for retention policies.
- **Returns**: `true` if valid; otherwise, `false`.

### IsValidConnectionTimeout
`public static bool IsValidConnectionTimeout(string? timeout)`
Validates whether the provided string represents a valid connection timeout duration.
- **Returns**: `true` if valid; otherwise, `false`.

## Usage

### Validating Configuration Settings
```csharp
string portInput = "5432";
string dbName = "tenant_db_01";

if (ValidationExtensions.IsValidPort(portInput) && ValidationExtensions.IsValidDatabaseName(dbName))
{
    // Proceed with database configuration initialization
}
```

### Validating Tenant Data
```csharp
string email = "admin@tenant.example.com";
string uuid = "550e8400-e29b-41d4-a716-446655440000";

if (!ValidationExtensions.IsValidEmail(email))
{
    throw new ArgumentException("Invalid administrator email.");
}

if (!ValidationExtensions.IsValidUuid(uuid))
{
    throw new ArgumentException("Invalid tenant identifier format.");
}
```

## Notes

- **Thread-Safety**: These methods are `static` and stateless, making them inherently thread-safe for concurrent access in multi-threaded environments.
- **Edge Cases**: All string-based validation methods gracefully handle `null` or empty inputs by returning `false`. It is recommended to perform null checks before passing data to these methods if specific null-handling logic (e.g., throwing an `ArgumentNullException`) is required by the calling code.
- **Performance**: While these validations are efficient, they should be used judiciously within high-frequency loops or performance-critical paths, as some may involve regular expression parsing.
