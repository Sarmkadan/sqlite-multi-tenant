# RequestCorrelationIdGenerator

Provides a static, ambient mechanism for generating, propagating, and retrieving a correlation identifier across asynchronous call chains within a multi-tenant SQLite application. It maintains a chain of correlation IDs to support nested request tracing and offers scope-based management for deterministic cleanup.

## API

### public static string GenerateCorrelationId()

Generates a new correlation identifier string and returns it. The implementation typically produces a unique, ordered value suitable for tracing a logical request. This method does not set the ambient correlation ID; use `SetCorrelationId` to associate the generated value with the current execution context.

- **Returns**: A new correlation ID string.
- **Throws**: No documented exceptions.

### public static void SetCorrelationId(string correlationId)

Sets the provided string as the ambient correlation ID for the current logical call context. If a correlation ID is already present, the new value is pushed onto the correlation chain, preserving the previous ID for nested scope tracking.

- **Parameters**:
  - `correlationId`: The correlation identifier to set. Must not be null.
- **Returns**: Void.
- **Throws**: `ArgumentNullException` if `correlationId` is null.

### public static string GetCorrelationId()

Retrieves the current ambient correlation ID. If no correlation ID has been set, returns null.

- **Returns**: The current correlation ID string, or null if none is set.
- **Throws**: No documented exceptions.

### public static bool HasCorrelationId()

Indicates whether a correlation ID is currently set in the ambient context.

- **Returns**: `true` if a correlation ID is present; otherwise `false`.
- **Throws**: No documented exceptions.

### public static List<string> GetCorrelationChain()

Returns the full chain of correlation IDs currently stored in the ambient context, ordered from outermost (oldest) to innermost (current). This enables tracing through nested logical scopes.

- **Returns**: A `List<string>` containing all correlation IDs in the chain. Returns an empty list if none are set.
- **Throws**: No documented exceptions.

### public static void ClearCorrelationId()

Removes the ambient correlation ID and clears the entire correlation chain for the current context. After calling this method, `HasCorrelationId` returns `false` and `GetCorrelationId` returns null.

- **Returns**: Void.
- **Throws**: No documented exceptions.

### public static IDisposable CreateScope(string correlationId)

Creates a new correlation scope by setting the specified correlation ID and returning an `IDisposable` instance. When the returned scope is disposed, the correlation context is restored to its state prior to the scope's creation. This is the recommended pattern for managing correlation IDs within bounded operations such as request handling.

- **Parameters**:
  - `correlationId`: The correlation ID to set for the duration of the scope. Must not be null.
- **Returns**: An `IDisposable` (`CorrelationIdScope`) that restores the previous correlation state upon disposal.
- **Throws**: `ArgumentNullException` if `correlationId` is null.

### public sealed class CorrelationIdScope

Represents a disposable scope that restores the ambient correlation context to its previous state. Instances are created via `CreateScope` and should not be instantiated directly.

- **Implements**: `IDisposable`

### public void Dispose()

Restores the ambient correlation ID and chain to the state that existed before the scope was created. If the scope was created when no correlation ID was set, disposal clears the correlation context.

- **Returns**: Void.
- **Throws**: No documented exceptions.

## Usage

### Example 1: Basic Request Handling with Scope

```csharp
public async Task HandleRequestAsync(HttpContext context)
{
    var correlationId = RequestCorrelationIdGenerator.GenerateCorrelationId();
    
    using (RequestCorrelationIdGenerator.CreateScope(correlationId))
    {
        Log.Information("Request started");
        
        await ProcessOrderAsync();
        await SendNotificationAsync();
        
        Log.Information("Request completed");
    }
    // Correlation context automatically restored here
}
```

### Example 2: Nested Scopes and Chain Inspection

```csharp
public async Task ExecuteWithNestedTracingAsync()
{
    var outerId = RequestCorrelationIdGenerator.GenerateCorrelationId();
    
    using (RequestCorrelationIdGenerator.CreateScope(outerId))
    {
        Console.WriteLine($"Outer: {RequestCorrelationIdGenerator.GetCorrelationId()}");
        
        var innerId = RequestCorrelationIdGenerator.GenerateCorrelationId();
        using (RequestCorrelationIdGenerator.CreateScope(innerId))
        {
            Console.WriteLine($"Inner: {RequestCorrelationIdGenerator.GetCorrelationId()}");
            
            var chain = RequestCorrelationIdGenerator.GetCorrelationChain();
            // chain[0] == outerId, chain[1] == innerId
            Console.WriteLine($"Chain depth: {chain.Count}");
        }
        // Inner scope disposed; outer ID restored
        Console.WriteLine($"Restored: {RequestCorrelationIdGenerator.GetCorrelationId()}");
    }
}
```

## Notes

- The type is sealed and all members are static, operating on ambient state stored in a context-aware carrier (e.g., `AsyncLocal<T>`). This ensures correlation IDs flow correctly across `await` boundaries within the same asynchronous control flow.
- `SetCorrelationId` pushes onto an existing chain rather than replacing it. Use `ClearCorrelationId` if you need to reset the chain entirely before setting a new root correlation ID.
- `CreateScope` is the preferred mechanism for temporary correlation ID assignment. It guarantees restoration of the prior state even if an exception occurs within the scope, provided the `using` statement or explicit `Dispose` call is employed.
- `GetCorrelationChain` returns a new `List<string>` snapshot. Modifying the returned list does not affect the ambient chain.
- Thread safety: The ambient state is isolated per logical execution context. Concurrent operations on different asynchronous flows do not interfere with each other. However, simultaneous calls to mutation methods (`SetCorrelationId`, `ClearCorrelationId`, `CreateScope`) from within the same logical context are not thread-safe and should be serialized by the caller.
