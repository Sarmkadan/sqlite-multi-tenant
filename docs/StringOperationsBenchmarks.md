# StringOperationsBenchmarks

`StringOperationsBenchmarks` is a performance benchmark class within the `SqliteMultiTenant.Benchmarks` namespace, designed to measure the execution time and memory allocation overhead of critical string-processing operations used during cache-key generation, file-path sanitization, and schema mapping in the `SqliteMultiTenant` library.

## API

*   `public string ComputeSha256Hash()`
    *   **Purpose:** Benchmarks the performance of `StringUtilities.ComputeSha256Hash` when processing a predefined tenant connection string.
    *   **Parameters:** None.
    *   **Return Value:** The SHA256 hash string of the internal benchmark input.
    *   **Exceptions:** May throw exceptions propagated from `StringUtilities.ComputeSha256Hash`.

*   `public string ComputeMd5Hash()`
    *   **Purpose:** Benchmarks the performance of `StringUtilities.ComputeMd5Hash` when processing a predefined tenant connection string.
    *   **Parameters:** None.
    *   **Return Value:** The MD5 hash string of the internal benchmark input.
    *   **Exceptions:** May throw exceptions propagated from `StringUtilities.ComputeMd5Hash`.

*   `public string ToSnakeCase()`
    *   **Purpose:** Benchmarks the performance of `StringUtilities.ToSnakeCase` when converting a camel-case string to snake-case.
    *   **Parameters:** None.
    *   **Return Value:** The snake-case version of the internal benchmark input.
    *   **Exceptions:** May throw exceptions propagated from `StringUtilities.ToSnakeCase`.

*   `public string ToCamelCase()`
    *   **Purpose:** Benchmarks the performance of `StringUtilities.ToCamelCase` when converting a snake-case string to camel-case.
    *   **Parameters:** None.
    *   **Return Value:** The camel-case version of the internal benchmark input.
    *   **Exceptions:** May throw exceptions propagated from `StringUtilities.ToCamelCase`.

*   `public string SanitizeForFilePath()`
    *   **Purpose:** Benchmarks the performance of `StringUtilities.SanitizeForFilePath` when cleaning a string for use as a file system path.
    *   **Parameters:** None.
    *   **Return Value:** The sanitized file path string derived from the internal benchmark input.
    *   **Exceptions:** May throw exceptions propagated from `StringUtilities.SanitizeForFilePath`.

## Usage

**Example 1: Running benchmarks using BenchmarkDotNet**

```csharp
using BenchmarkDotNet.Running;
using SqliteMultiTenant.Benchmarks;

// Run the benchmarks
var summary = BenchmarkRunner.Run<StringOperationsBenchmarks>();
```

**Example 2: Manual invocation for verification**

```csharp
using SqliteMultiTenant.Benchmarks;

var benchmarks = new StringOperationsBenchmarks();

// Manually invoke a benchmark method
string sha256 = benchmarks.ComputeSha256Hash();
string snakeCase = benchmarks.ToSnakeCase();

Console.WriteLine($"SHA256: {sha256}");
Console.WriteLine($"SnakeCase: {snakeCase}");
```

## Notes

*   **Benchmark Framework:** This class is specifically designed to be executed by the `BenchmarkDotNet` framework. Results may not reflect real-world usage patterns if invoked manually outside of a controlled benchmarking environment.
*   **Thread Safety:** The methods within this class are not thread-safe. They are intended for use in isolated benchmark runs controlled by the `BenchmarkDotNet` runner, which ensures sequential execution and measurement accuracy.
*   **Edge Cases:** The benchmarks utilize static, predefined input strings. They do not cover edge cases such as `null`, empty, or extremely large input strings. The behavior of these methods under such conditions depends entirely on the underlying `StringUtilities` implementation.
