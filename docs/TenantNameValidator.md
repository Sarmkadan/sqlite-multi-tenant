# TenantNameValidator

The `TenantNameValidator` static class provides utility methods for validating and generating identifiers and names for tenants in a `sqlite-multi-tenant` application, ensuring compatibility with the underlying storage mechanisms and filesystem requirements.

## API

### ValidationResult
A sealed class representing the outcome of a validation operation.

*   `public bool IsValid`
    Indicates whether the validation was successful.
*   `public string Error`
    Contains a descriptive error message if validation failed; otherwise, null or empty.

### Methods

*   `public static ValidationResult ValidateTenantId(string tenantId)`
    Validates the format of a tenant identifier. Returns a `ValidationResult` indicating success or failure. Throws `ArgumentNullException` if `tenantId` is null.
*   `public static ValidationResult ValidateTenantName(string tenantName)`
    Validates the format of a tenant name. Returns a `ValidationResult` indicating success or failure. Throws `ArgumentNullException` if `tenantName` is null.
*   `public static string GenerateTenantId()`
    Generates a valid, formatted tenant identifier. Returns a string representation of the generated identifier.
*   `public static bool IsValidDatabaseIdentifier(string identifier)`
    Determines whether a string is a valid database identifier, ensuring it conforms to naming constraints for SQLite tables or filenames. Returns true if the identifier is valid; otherwise, false. Throws `ArgumentNullException` if `identifier` is null.

## Usage

### Validating a Tenant Identifier
```csharp
string inputId = "custom_tenant_01";
ValidationResult result = TenantNameValidator.ValidateTenantId(inputId);

if (result.IsValid)
{
    Console.WriteLine("Tenant identifier is valid.");
}
else
{
    Console.WriteLine($"Invalid identifier: {result.Error}");
}
```

### Generating a New Tenant Identifier
```csharp
string newTenantId = TenantNameValidator.GenerateTenantId();
// Use the generated ID to initialize a new tenant database
Console.WriteLine($"Generated new tenant ID: {newTenantId}");
```

## Notes

*   **Thread Safety:** The `TenantNameValidator` class is stateless and thread-safe. All methods can be called concurrently from multiple threads without synchronization.
*   **Input Handling:** Methods accepting string inputs will throw `ArgumentNullException` if a `null` argument is provided. Empty or whitespace-only strings are generally treated as invalid.
*   **Database Constraints:** `IsValidDatabaseIdentifier` checks against restrictions specific to the filesystem and SQLite's database naming rules; identifiers that pass this validation are guaranteed to be safe for use as database filenames or table names within the supported environment.
