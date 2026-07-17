# DataRetentionPolicyJsonExtensions

Provides static extension methods for serializing and deserializing data retention policy types (`RetentionPolicyConfig`, `RetentionRule`, `RetentionResult`, `RuleExecutionResult`) to and from JSON strings. All methods are stateless and operate solely on their input parameters.

## API

### RetentionPolicyConfig

#### `ToJson(this RetentionPolicyConfig config)`

Serializes a `RetentionPolicyConfig` instance to its JSON representation.

- **Parameters**: `config` – The configuration to serialize.
- **Returns**: A JSON string.
- **Throws**: `ArgumentNullException` if `config` is `null`.

#### `FromJson(string json)`

Deserializes a JSON string into a `RetentionPolicyConfig` instance.

- **Parameters**: `json` – The JSON string to deserialize.
- **Returns**: A `RetentionPolicyConfig?` value; `null` if the JSON is `null` or empty.
- **Throws**: `JsonException` if the JSON is malformed or cannot be mapped to the target type.

#### `TryFromJson(string json, out RetentionPolicyConfig? result)`

Attempts to deserialize a JSON string into a `RetentionPolicyConfig` instance without throwing exceptions.

- **Parameters**:
  - `json` – The JSON string to attempt deserialization.
  - `result` – When this method returns, contains the deserialized value or `null` if deserialization failed.
- **Returns**: `true` if deserialization succeeded; otherwise `false`.

### RetentionRule

#### `ToJson(this RetentionRule rule)`

Serializes a `RetentionRule` instance to its JSON representation.

- **Parameters**: `rule` – The rule to serialize.
- **Returns**: A JSON string.
- **Throws**: `ArgumentNullException` if `rule` is `null`.

#### `FromJsonToRule(string json)`

Deserializes a JSON string into a `RetentionRule` instance.

- **Parameters**: `json` – The JSON string to deserialize.
- **Returns**: A `RetentionRule?` value; `null` if the JSON is `null` or empty.
- **Throws**: `JsonException` if the JSON is malformed or cannot be mapped to the target type.

#### `TryFromJson(string json, out RetentionRule? result)`

Attempts to deserialize a JSON string into a `RetentionRule` instance without throwing exceptions.

- **Parameters**:
  - `json` – The JSON string to attempt deserialization.
  - `result` – When this method returns, contains the deserialized value or `null` if deserialization failed.
- **Returns**: `true` if deserialization succeeded; otherwise `false`.

### RetentionResult

#### `ToJson(this RetentionResult result)`

Serializes a `RetentionResult` instance to its JSON representation.

- **Parameters**: `result` – The result to serialize.
- **Returns**: A JSON string.
- **Throws**: `ArgumentNullException` if `result` is `null`.

#### `FromJsonToResult(string json)`

Deserializes a JSON string into a `RetentionResult` instance.

- **Parameters**: `json` – The JSON string to deserialize.
- **Returns**: A `RetentionResult?` value; `null` if the JSON is `null` or empty.
- **Throws**: `JsonException` if the JSON is malformed or cannot be mapped to the target type.

#### `TryFromJson(string json, out RetentionResult? result)`

Attempts to deserialize a JSON string into a `RetentionResult` instance without throwing exceptions.

- **Parameters**:
  - `json` – The JSON string to attempt deserialization.
  - `result` – When this method returns, contains the deserialized value or `null` if deserialization failed.
- **Returns**: `true` if deserialization succeeded; otherwise `false`.

### RuleExecutionResult

#### `ToJson(this RuleExecutionResult executionResult)`

Serializes a `RuleExecutionResult` instance to its JSON representation.

- **Parameters**: `executionResult` – The execution result to serialize.
- **Returns**: A JSON string.
- **Throws**: `ArgumentNullException` if `executionResult` is `null`.

#### `FromJsonToExecutionResult(string json)`

Deserializes a JSON string into a `RuleExecutionResult` instance.

- **Parameters**: `json` – The JSON string to deserialize.
- **Returns**: A `RuleExecutionResult?` value; `null` if the JSON is `null` or empty.
- **Throws**: `JsonException` if the JSON is malformed or cannot be mapped to the target type.

#### `TryFromJson(string json, out RuleExecutionResult? result)`

Attempts to deserialize a JSON string into a `RuleExecutionResult` instance without throwing exceptions.

- **Parameters**:
  - `json` – The JSON string to attempt deserialization.
  - `result` – When this method returns, contains the deserialized value or `null` if deserialization failed.
- **Returns**: `true` if deserialization succeeded; otherwise `false`.

## Usage

### Example 1: Serialize and deserialize a retention policy configuration

```csharp
using SqliteMultiTenant.DataRetention;

var config = new RetentionPolicyConfig
{
    DefaultRetentionDays = 90,
    Rules = new List<RetentionRule>
    {
        new RetentionRule { TableName = "Logs", RetentionDays = 30 }
    }
};

// Serialize to JSON
string json = config.ToJson();

// Deserialize back
RetentionPolicyConfig? restored = json.FromJson();
if (restored != null)
{
    Console.WriteLine($"Default retention: {restored.DefaultRetentionDays} days");
}
```

### Example 2: Safely attempt deserialization of a retention rule

```csharp
using SqliteMultiTenant.DataRetention;

string json = @"{ ""TableName"": ""Audit"", ""RetentionDays"": 60 }";

if (json.TryFromJson(out RetentionRule? rule))
{
    Console.WriteLine($"Rule for table '{rule.TableName}' with {rule.RetentionDays} days retention.");
}
else
{
    Console.WriteLine("Failed to parse retention rule JSON.");
}
```

## Notes

- All `ToJson` methods throw `ArgumentNullException` when the source object is `null`. Always ensure the input is non-null before calling.
- The `FromJson` methods return `null` when the input JSON string is `null` or empty. They throw `JsonException` for malformed JSON or type mismatches.
- The `TryFromJson` methods never throw; they return `false` and set the output parameter to `null` on any failure (including `null` or empty input, malformed JSON, or type mismatch).
- All methods are static and do not maintain internal state. They are thread-safe and can be called concurrently from multiple threads without synchronization.
- JSON serialization uses the default `System.Text.Json` serializer settings. Custom converters or options are not applied.
