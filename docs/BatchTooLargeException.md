# BatchTooLargeException

A sealed exception type thrown when a batch operation exceeds configured item-count or payload-size limits. It records both the configured limits and the actual batch measurements so callers can diagnose and handle oversized requests.

## API

### Properties

#### `MaxItemCount`
`public int MaxItemCount { get; }`

Gets the maximum number of items allowed in the batch.

#### `ActualItemCount`
`public int ActualItemCount { get; }`

Gets the actual number of items in the batch.

#### `MaxPayloadSizeBytes`
`public long MaxPayloadSizeBytes { get; }`

Gets the maximum allowed payload size in bytes. This value is `0` when the constructor does not accept payload-size information.

#### `ActualPayloadSizeBytes`
`public long ActualPayloadSizeBytes { get; }`

Gets the actual payload size in bytes. This value is `0` when the constructor does not accept payload-size information.

### Constructors

#### `BatchTooLargeException(int maxItemCount, int actualItemCount)`
```csharp
public BatchTooLargeException(int maxItemCount, int actualItemCount)
```

Initializes a new instance with item-count information. The exception message is generated automatically, and both payload-size properties are set to `0`.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `maxItemCount` | `int` | The maximum number of items allowed in the batch. |
| `actualItemCount` | `int` | The actual number of items in the batch. |

#### `BatchTooLargeException(int maxItemCount, int actualItemCount, long maxPayloadSizeBytes, long actualPayloadSizeBytes)`
```csharp
public BatchTooLargeException(
    int maxItemCount,
    int actualItemCount,
    long maxPayloadSizeBytes,
    long actualPayloadSizeBytes)
```

Initializes a new instance with item-count and payload-size information. The exception message is generated automatically and includes payload sizes when both payload arguments are greater than zero.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `maxItemCount` | `int` | The maximum number of items allowed in the batch. |
| `actualItemCount` | `int` | The actual number of items in the batch. |
| `maxPayloadSizeBytes` | `long` | The maximum payload size allowed, in bytes. |
| `actualPayloadSizeBytes` | `long` | The actual payload size, in bytes. |

#### `BatchTooLargeException(string message, int maxItemCount, int actualItemCount)`
```csharp
public BatchTooLargeException(
    string message,
    int maxItemCount,
    int actualItemCount)
```

Initializes a new instance with a custom message and item-count information. Both payload-size properties are set to `0`.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `message` | `string` | The message that describes the error condition. |
| `maxItemCount` | `int` | The maximum number of items allowed in the batch. |
| `actualItemCount` | `int` | The actual number of items in the batch. |

#### `BatchTooLargeException(string message, int maxItemCount, int actualItemCount, Exception? innerException)`
```csharp
public BatchTooLargeException(
    string message,
    int maxItemCount,
    int actualItemCount,
    Exception? innerException)
```

Initializes a new instance with a custom message, item-count information, and an optional inner exception. Both payload-size properties are set to `0`.

| Parameter | Type | Purpose |
|-----------|------|---------|
| `message` | `string` | The message that describes the error condition. |
| `maxItemCount` | `int` | The maximum number of items allowed in the batch. |
| `actualItemCount` | `int` | The actual number of items in the batch. |
| `innerException` | `Exception?` | The exception that caused the current exception, or `null`. |

## Usage

### Rejecting and Inspecting an Oversized Batch

```csharp
const int maxItemCount = 100;
const long maxPayloadSizeBytes = 1_048_576;

int actualItemCount = items.Count;
long actualPayloadSizeBytes = payload.Length;

try
{
    if (actualItemCount > maxItemCount || actualPayloadSizeBytes > maxPayloadSizeBytes)
    {
        throw new BatchTooLargeException(
            maxItemCount,
            actualItemCount,
            maxPayloadSizeBytes,
            actualPayloadSizeBytes);
    }

    await ProcessBatchAsync(items, payload);
}
catch (BatchTooLargeException ex)
{
    Console.WriteLine(
        $"Batch contained {ex.ActualItemCount}/{ex.MaxItemCount} items and " +
        $"{ex.ActualPayloadSizeBytes}/{ex.MaxPayloadSizeBytes} bytes.");
}
```

## Notes

- The automatically generated message always reports the maximum and actual item counts.
- Payload sizes are added to the generated message only when both `maxPayloadSizeBytes` and `actualPayloadSizeBytes` are greater than zero.
- Constructors without payload-size parameters initialize `MaxPayloadSizeBytes` and `ActualPayloadSizeBytes` to `0`.
- Payload sizes in generated messages are formatted using binary multiples of 1,024 and the labels `B`, `KB`, `MB`, `GB`, and `TB`.
- This type is sealed and cannot be subclassed.
