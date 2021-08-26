# OptionsValidatorBenchmarks

Benchmarking harness for validating configuration options in the `sqlite-multi-tenant` library. This class measures the performance of option validation logic under various scenarios, including multi-tenant and backup configuration validations.

## API

### `Setup`
Initializes the benchmark context before each test iteration. Sets up the necessary dependencies and test data required for validation benchmarks.

### `ValidateMultiTenantOptions_Valid`
Validates a set of valid multi-tenant configuration options and measures the execution time. This benchmark ensures that the validation logic correctly processes well-formed multi-tenant configurations without throwing exceptions.

### `ValidateBackupOptions_Valid`
Validates a set of valid backup configuration options and measures the execution time. This benchmark ensures that the validation logic correctly processes well-formed backup configurations without throwing exceptions.

## Usage
