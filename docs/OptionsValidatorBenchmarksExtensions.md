# OptionsValidatorBenchmarksExtensions

The `OptionsValidatorBenchmarksExtensions` class provides a specialized suite of static extension methods designed to facilitate benchmarking, performance profiling, and robust error handling for options validation within the `sqlite-multi-tenant` framework. These utilities allow developers to measure the execution time of validation logic, execute comprehensive validation suites, generate formatted reports, and implement resilient validation strategies using retry mechanisms, ensuring that configuration validation is both performant and reliable during system initialization or hot-reloading scenarios.

## API

### RunAllValidations
Executes all registered validation rules against the target options configuration. Returns a summary string detailing the outcome of all validation checks.

### MeasureValidation
Executes the specified validation logic and records the elapsed time. Returns a `TimeSpan` representing the duration required to complete the validation process.

### ValidateAndReport
Performs validation on the target options and produces a detailed string report, including any validation errors or warnings encountered. This is useful for logging configuration issues during application startup.

### ValidateWithRetry
Attempts to validate the options configuration, utilizing a configurable retry strategy. Returns `true` if validation succeeds within the allotted attempts; otherwise, returns `false`.

## Usage

```csharp
// Example 1: Measuring and reporting validation performance
var options = GetOptions();
var duration = OptionsValidatorBenchmarksExtensions.MeasureValidation(options);

Console.WriteLine($"Validation completed in {duration.TotalMilliseconds}ms.");

string report = OptionsValidatorBenchmarksExtensions.ValidateAndReport(options);
Console.WriteLine(report);
```

```csharp
// Example 2: Using retry logic for resilient validation
var options = GetOptions();
bool isValid = OptionsValidatorBenchmarksExtensions.ValidateWithRetry(options);

if (!isValid)
{
    // Log failure after retries
    throw new InvalidOperationException("Options validation failed after multiple attempts.");
}
```

## Notes

*   **Thread Safety:** The methods within this class are designed to be thread-safe when invoked concurrently, provided the underlying validation rules and the options being validated themselves are also thread-safe.
*   **Performance:** `MeasureValidation` should be used judiciously in production environments, as constant profiling may introduce minor overhead. It is primarily intended for benchmark suites and diagnostic logging.
*   **Retry Policy:** `ValidateWithRetry` relies on the configured retry settings. If the validation logic is inherently non-deterministic or depends on external state that does not change between retries, the retry mechanism may not resolve the underlying issue.
*   **Error Handling:** While `ValidateAndReport` and `ValidateWithRetry` handle common validation scenarios, they do not catch exceptions thrown by the underlying validation logic itself. Ensure that the validation rules being invoked are robust.
