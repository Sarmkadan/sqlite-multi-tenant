# TenantValidator

TenantValidator and its associated validator classes provide a centralized suite of validation services for the `sqlite-multi-tenant` framework, ensuring that tenant configurations, database migrations, SQLite connection strings, and backup settings conform to operational requirements, thereby maintaining system integrity during tenant management operations.

## API

### TenantValidator
Provides validation logic for tenant-related requests.

*   `ValidateCreateRequest(TenantCreateRequest request)`
    Validates a request to create a new tenant. Returns a `Dictionary<string, string>` where keys are field names and values are validation error messages.
*   `ValidateUpdateRequest(TenantUpdateRequest request)`
    Validates a request to update an existing tenant. Returns a `Dictionary<string, string>` containing validation errors.
*   `ValidateNameUniqueness(string tenantName)`
    Checks if a tenant name is unique within the system. Returns a `Dictionary<string, string>` containing validation errors if the name is already in use.

### MigrationValidator
Provides validation for database migration processes.

*   `ValidateMigrationRequest(MigrationRequest request)`
    Validates a request for a database migration. Returns a `Dictionary<string, string>` containing validation errors.
*   `IsValidMigrationNaming(string migrationName)`
    Verifies if a migration name adheres to established naming conventions. Returns `true` if valid, otherwise `false`.
*   `ContainsDangerousPatterns(string script)`
    Analyzes a migration script for potentially dangerous SQL patterns. Returns `true` if dangerous patterns are detected, otherwise `false`.

### ConnectionStringValidator
Validates SQLite-specific connection strings.

*   `ValidateSqliteConnectionString(string connectionString)`
    Validates the format and security constraints of a SQLite connection string. Returns a `Dictionary<string, string>` containing validation errors.

### BackupValidator
Provides validation for tenant backup configurations.

*   `ValidateBackupTag(string backupTag)`
    Validates a backup tag string for format and length constraints. Returns a `Dictionary<string, string>` containing validation errors.
*   `ValidateRetentionDays(int retentionDays)`
    Validates the retention period for backups. Returns a `Dictionary<string, string>` containing validation errors if the value is outside the allowed range.

## Usage

### Example 1: Validating a Tenant Creation Request
```csharp
var validator = new TenantValidator();
var request = new TenantCreateRequest { Name = "NewTenant", ConnectionString = "Data Source=tenant.db" };
var errors = validator.ValidateCreateRequest(request);

if (errors.Any())
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Field: {error.Key}, Error: {error.Value}");
    }
}
else
{
    // Proceed with tenant creation
}
```

### Example 2: Checking Migration Naming and Safety
```csharp
var migrationValidator = new MigrationValidator();
string migrationName = "20260711_AddUserTable";
string script = "DROP TABLE Users;"; // Potentially dangerous

if (migrationValidator.IsValidMigrationNaming(migrationName))
{
    if (!migrationValidator.ContainsDangerousPatterns(script))
    {
        // Proceed with migration
    }
    else
    {
        Console.WriteLine("Migration script contains dangerous patterns.");
    }
}
```

## Notes

*   **Thread Safety:** The validator classes are designed to be stateless and are thread-safe for concurrent use.
*   **Error Handling:** The methods returning `Dictionary<string, string>` return an empty dictionary if the input is valid.
*   **Input Sanitization:** While these validators check for format and naming constraints, callers should still perform appropriate sanitization before executing SQL commands or interacting with the file system to prevent injection or path traversal attacks.
