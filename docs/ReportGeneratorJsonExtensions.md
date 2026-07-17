# ReportGeneratorJsonExtensions

Provides extension methods for JSON serialization and deserialization of report-related types in the `sqlite-multi-tenant` project. The class offers strongly-typed conversions between JSON strings and domain objects such as `SystemHealthSummary`, `OperationStatistics`, and `PerformanceMetric`, as well as safe try-pattern variants for error handling.

## API

### `ToJson` (overload 1)

```csharp
public static string ToJson(this SystemHealthSummary? value)
```

Serializes a `SystemHealthSummary` instance to its JSON representation.

- **Parameters**  
  `value` – The object to serialize. May be `null`.
- **Returns**  
  A JSON string. If `value` is `null`, returns the JSON literal `null`.
- **Throws**  
  `JsonException` if the object cannot be serialized (e.g., circular references or invalid data).

### `ToJson` (overload 2)

```csharp
public static string ToJson(this OperationStatistics? value)
```

Serializes an `OperationStatistics` instance to its JSON representation.

- **Parameters**  
  `value` – The object to serialize. May be `null`.
- **Returns**  
  A JSON string. If `value` is `null`, returns the JSON literal `null`.
- **Throws**  
  `JsonException` if the object cannot be serialized.

### `ToJson` (overload 3)

```csharp
public static string ToJson(this PerformanceMetric? value)
```

Serializes a `PerformanceMetric` instance to its JSON representation.

- **Parameters**  
  `value` – The object to serialize. May be `null`.
- **Returns**  
  A JSON string. If `value` is `null`, returns the JSON literal `null`.
- **Throws**  
  `JsonException` if the object cannot be serialized.

### `FromJsonToHealthSummary`

```csharp
public static SystemHealthSummary? FromJsonToHealthSummary(this string json)
```

Deserializes a JSON string into a `SystemHealthSummary` object.

- **Parameters**  
  `json` – The JSON string to deserialize. Must not be `null`.
- **Returns**  
  A `SystemHealthSummary` instance, or `null` if the JSON represents a null value.
- **Throws**  
  `ArgumentNullException` if `json` is `null`.  
  `JsonException` if the JSON is invalid or cannot be mapped to `SystemHealthSummary`.

### `FromJsonToOperationStatistics`

```csharp
public static System.Collections.Generic.IEnumerable<OperationStatistics>? FromJsonToOperationStatistics(this string json)
```

Deserializes a JSON string into a collection of `OperationStatistics` objects.

- **Parameters**  
  `json` – The JSON string to deserialize. Must not be `null`.
- **Returns**  
  An `IEnumerable<OperationStatistics>` if the JSON represents an array, or `null` if the JSON represents a null value. The returned collection is materialized.
- **Throws**  
  `ArgumentNullException` if `json` is `null`.  
  `JsonException` if the JSON is invalid or cannot be mapped to the expected collection type.

### `FromJsonToPerformanceMetrics`

```csharp
public static System.Collections.Generic.IEnumerable<PerformanceMetric>? FromJsonToPerformanceMetrics(this string json)
```

Deserializes a JSON string into a collection of `PerformanceMetric` objects.

- **Parameters**  
  `json` – The JSON string to deserialize. Must not be `null`.
- **Returns**  
  An `IEnumerable<PerformanceMetric>` if the JSON represents an array, or `null` if the JSON represents a null value. The returned collection is materialized.
- **Throws**  
  `ArgumentNullException` if `json` is `null`.  
  `JsonException` if the JSON is invalid or cannot be mapped to the expected collection type.

### `TryFromJson` (overload 1)

```csharp
public static bool TryFromJson(this string json, out SystemHealthSummary? result)
```

Attempts to deserialize a JSON string into a `SystemHealthSummary` object without throwing exceptions.

- **Parameters**  
  `json` – The JSON string to deserialize. May be `null`.  
  `result` – When this method returns, contains the deserialized object, or `null` if deserialization failed or the JSON represents a null value.
- **Returns**  
  `true` if deserialization succeeded; otherwise `false`. A `null` JSON string or a JSON `null` literal returns `true` with `result` set to `null`.

### `TryFromJson` (overload 2)

```csharp
public static bool TryFromJson(this string json, out System.Collections.Generic.IEnumerable<OperationStatistics>? result)
```

Attempts to deserialize a JSON string into a collection of `OperationStatistics` objects without throwing exceptions.

- **Parameters**  
  `json` – The JSON string to deserialize. May be `null`.  
  `result` – When this method returns, contains the deserialized collection, or `null` if deserialization failed or the JSON represents a null value.
- **Returns**  
  `true` if deserialization succeeded; otherwise `false`.

### `TryFromJson` (overload 3)

```csharp
public static bool TryFromJson(this string json, out System.Collections.Generic.IEnumerable<PerformanceMetric>? result)
```

Attempts to deserialize a JSON string into a collection of `PerformanceMetric` objects without throwing exceptions.

- **Parameters**  
  `json` – The JSON string to deserialize. May be `null`.  
  `result` – When this method returns, contains the deserialized collection, or `null` if deserialization failed or the JSON represents a null value.
- **Returns**  
  `true` if deserialization succeeded; otherwise `false`.

## Usage

### Example 1: Serializing and deserializing a health summary

```csharp
using System;
using SqliteMultiTenant.Reporting;

var health = new SystemHealthSummary
{
    DatabaseStatus = "Healthy",
    LastChecked = DateTime.UtcNow
};

// Serialize to JSON
string json = health.ToJson();
Console.WriteLine(json);

// Deserialize back
if (json.TryFromJson(out SystemHealthSummary? restored))
{
    Console.WriteLine($"Status: {restored?.DatabaseStatus}");
}
else
{
    Console.WriteLine("Deserialization failed.");
}
```

### Example 2: Working with collections of performance metrics

```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Reporting;

var metrics = new List<PerformanceMetric>
{
    new PerformanceMetric { Name = "CPU", Value = 45.2 },
    new PerformanceMetric { Name = "Memory", Value = 1024 }
};

// Serialize the list
string json = metrics.ToJson();

// Deserialize using the try-pattern
if (json.TryFromJson(out IEnumerable<PerformanceMetric>? restoredMetrics))
{
    foreach (var m in restoredMetrics ?? Enumerable.Empty<PerformanceMetric>())
    {
        Console.WriteLine($"{m.Name}: {m.Value}");
    }
}
else
{
    Console.WriteLine("Failed to deserialize metrics.");
}
```

## Notes

- **Null handling**: All `ToJson` methods accept `null` and return the JSON literal `null`. The `FromJson*` methods return `null` when the JSON represents a null value. The `TryFromJson` overloads treat a `null` JSON string as a successful deserialization with a `null` result.
- **Invalid JSON**: The non-try methods throw `JsonException` on malformed JSON or type mismatches. The `TryFromJson` methods return `false` and set `result` to `null` in such cases.
- **Thread safety**: All members are static and do not modify any shared state. They are safe to call concurrently from multiple threads, provided the input arguments are not mutated during the call.
- **Performance**: Deserialization of collections (`FromJsonToOperationStatistics`, `FromJsonToPerformanceMetrics`) materializes the entire collection immediately. For very large JSON arrays, consider streaming alternatives if memory usage is a concern.
