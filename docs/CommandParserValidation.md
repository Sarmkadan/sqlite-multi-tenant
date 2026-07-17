# CommandParserValidation

A utility class providing static validation methods for command parsing operations in multi-tenant SQLite environments. Ensures that parsed commands meet required formatting and security standards before execution, preventing invalid or potentially harmful database operations across tenant boundaries.

## API

### Validate

Multiple overloads exist to validate different command types. Each returns an `IReadOnlyList<string>` containing error messages for any validation failures, or an empty list if the command is valid.

**Purpose:** Performs validation checks on command inputs and returns detailed error information.

**Parameters:** Varies by overload - typically accepts command strings, parsed command objects, or tenant-specific command parameters.

**Return Value:** `IReadOnlyList<string>` - Collection of validation error messages; empty when no errors are found.

**Exceptions:** Does not throw exceptions; returns error messages in the result collection.

### IsValid

Multiple overloads corresponding to Validate methods. Returns a boolean indicating whether the provided command passes all validation checks.

**Purpose:** Quick validation check that returns true for valid commands, false otherwise.

**Parameters:** Varies by overload - accepts the same input types as corresponding Validate methods.

**Return Value:** `bool` - True if command is valid, false if any validation errors exist.

**Exceptions:** Does not throw exceptions.

### EnsureValid

Multiple overloads that perform validation and throw an exception if the command is invalid.

**Purpose:** Validates command and throws InvalidOperationException if validation fails, ensuring only valid commands proceed.

**Parameters:** Varies by overload - accepts the same input types as corresponding Validate methods.

**Return Value:** `void`

**Exceptions:** Throws `InvalidOperationException` when validation fails, containing the validation error messages.

## Usage

```csharp
// Example 1: Validate a SQL command before execution
var sqlCommand = "SELECT * FROM users WHERE tenant_id = @tenantId";
var errors = CommandParserValidation.Validate(sqlCommand, tenantContext);

if (errors.Any())
{
    // Log validation errors
    foreach (var error in errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}
else
{
    // Proceed with command execution
    ExecuteCommand(sqlCommand);
}
```

```csharp
// Example 2: Use EnsureValid to guarantee command validity
try
{
    CommandParserValidation.EnsureValid(parsedCommand, tenantId);
    // Command is guaranteed valid - safe to execute
    ExecuteParsedCommand(parsedCommand);
}
catch (InvalidOperationException ex)
{
    // Handle invalid command scenario
    LogValidationError(ex.Message);
    throw new BadRequestException("Invalid database command", ex);
}
```

## Notes

All methods are static and thread-safe, making them suitable for concurrent use in multi-threaded applications. The validation logic does not maintain state between calls, ensuring consistent behavior regardless of invocation context.

Empty or null command inputs may result in validation failures depending on the specific overload used. Callers should ensure appropriate input sanitization before invoking these methods to avoid unexpected validation results.

The multiple overloads suggest support for different command formats (raw SQL strings, parsed command objects, etc.) but specific parameter signatures depend on the actual implementation details not provided in the member listing.
