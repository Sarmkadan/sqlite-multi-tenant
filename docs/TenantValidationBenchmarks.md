# TenantValidationBenchmarks

The `TenantValidationBenchmarks` class provides a set of benchmark scenarios for validating tenant identifiers and names in a multi-tenant SQLite environment. It is designed to be used with a benchmarking framework (e.g., BenchmarkDotNet) to measure the performance of validation logic under different conditions, including valid inputs, reserved keywords, SQL injection attempts, and tenant name validation. The class also exposes a method to generate synthetic tenant IDs for use in benchmarks or test data.

## API

### `public sealed class TenantValidationBenchmarks`

The class is sealed and cannot be inherited. It contains no instance constructors that are publicly visible; instantiation is typically handled by the benchmarking framework.

### `public ValidationResult ValidateTenantId_Valid`

- **Purpose**: Validates a tenant ID that conforms to all expected rules (e.g., correct length, allowed characters, not reserved).
- **Parameters**: None (property getter).
- **Return value**: A `ValidationResult` indicating success (valid) or failure (invalid) for the valid tenant ID scenario.
- **Throws**: Does not throw. The property is expected to always return a result without side effects.

### `public ValidationResult ValidateTenantId_Reserved`

- **Purpose**: Validates a tenant ID that matches a reserved keyword (e.g., "admin", "system") that should be rejected.
- **Parameters**: None.
- **Return value**: A `ValidationResult` indicating failure (invalid) because the ID is reserved.
- **Throws**: Does not throw.

### `public ValidationResult ValidateTenantId_SqlInjection`

- **Purpose**: Validates a tenant ID that contains SQL injection patterns (e.g., `' OR 1=1 --`) to ensure the validation logic rejects such inputs.
- **Parameters**: None.
- **Return value**: A `ValidationResult` indicating failure (invalid) due to the presence of dangerous characters or patterns.
- **Throws**: Does not throw.

### `public ValidationResult ValidateTenantName`

- **Purpose**: Validates a tenant name (as opposed to an ID) against the applicable rules (e.g., length, allowed characters, no reserved names).
- **Parameters**: None.
- **Return value**: A `ValidationResult` indicating whether the sample tenant name passes validation.
- **Throws**: Does not throw.

### `public string GenerateTenantId`

- **Purpose**: Generates a random, syntactically valid tenant ID that can be used as test data or input for benchmarks.
- **Parameters**: None.
- **Return value**: A `string` representing a newly generated tenant ID. The ID is guaranteed to be unique per call (within reasonable probability) and to pass basic validation rules.
- **Throws**: Does not throw.

## Usage

### Example 1: Running benchmarks with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Running;
using SqliteMultiTenant.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<TenantValidationBenchmarks>();
        Console.WriteLine(summary);
    }
}
```

This example runs all benchmark properties (`ValidateTenantId_Valid`, `ValidateTenantId_Reserved`, `ValidateTenantId_SqlInjection`, `ValidateTenantName`) and the `GenerateTenantId` method, measuring their execution time and memory allocation.

### Example 2: Direct usage for validation testing

```csharp
using SqliteMultiTenant.Benchmarks;

var benchmarks = new TenantValidationBenchmarks();

// Check validation results
Console.WriteLine($"Valid ID: {benchmarks.ValidateTenantId_Valid.IsValid}");
Console.WriteLine($"Reserved ID: {benchmarks.ValidateTenantId_Reserved.IsValid}");
Console.WriteLine($"SQL injection ID: {benchmarks.ValidateTenantId_SqlInjection.IsValid}");
Console.WriteLine($"Tenant name: {benchmarks.ValidateTenantName.IsValid}");

// Generate a new tenant ID
string newId = benchmarks.GenerateTenantId();
Console.WriteLine($"Generated tenant ID: {newId}");
```

This example instantiates the class directly and accesses each property and method to inspect the validation outcomes and generate a sample ID.

## Notes

- **Edge cases**: The `ValidateTenantId_Reserved` property tests a tenant ID that matches a reserved keyword (e.g., "admin", "root", "system"). The `ValidateTenantId_SqlInjection` property tests inputs containing SQL metacharacters such as single quotes, dashes, and semicolons. The exact reserved list and injection patterns are defined internally and may be extended in future versions.
- **Thread safety**: This class is not designed for concurrent access. Each property getter and the `GenerateTenantId` method may rely on shared state (e.g., random number generators) that is not synchronized. For benchmarking, the framework typically ensures single-threaded execution per iteration. Direct use in multi-threaded scenarios should be avoided or protected with external synchronization.
- **ValidationResult**: The `ValidationResult` type is assumed to expose an `IsValid` boolean property and possibly an `Errors` collection. The exact API depends on the validation library used (e.g., FluentValidation's `ValidationResult`). The benchmark properties return pre-configured results; they do not accept parameters or perform dynamic validation.
- **Benchmarking considerations**: The properties are expected to be cheap to evaluate (micro-benchmarks). The `GenerateTenantId` method may involve random number generation and string allocation, which will be reflected in memory allocation measurements.
