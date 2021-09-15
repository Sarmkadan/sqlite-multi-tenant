# CommandExecutor
The `CommandExecutor` type encapsulates the execution of a single SQL command against a tenant‑specific SQLite database, returning a `CommandResult` that indicates whether the operation succeeded and provides an informational or error message.

## API
### CommandExecutor()
Initializes a new instance of the `CommandExecutor`. The constructor does not take any parameters and prepares the executor for use. No exceptions are thrown under normal circumstances.

### ExecuteAsync()
Asynchronously executes the configured SQL command.

- **Parameters:** None.
- **Return value:** A `Task<CommandResult>` that completes when the command has finished executing. The resulting `CommandResult` contains a `Success` flag and a `Message` describing the outcome.
- **Exceptions:** May propagate any exception thrown by the underlying SQLite provider (e.g., `SQLiteException` for syntax or constraint errors, `ObjectDisposedException` if the associated connection has been disposed, or `OperationCanceledException` if a cancellation token is triggered elsewhere). If the task faults, the exception is propagated directly from the returned task.

### CommandResult
A sealed class that represents the outcome of a command execution.

### Success
Gets a boolean value indicating whether the command completed successfully (`true`) or failed (`false`).

### Message
Gets a string that provides additional information about the execution. On success, this may contain a confirmation or row‑count message; on failure, it typically contains an error description.

## Usage
```csharp
using System.Threading.Tasks;

// Assume executor is already configured for a specific tenant.
var executor = new CommandExecutor();

CommandResult result = await executor.ExecuteAsync();

if (result.Success)
{
    // Command succeeded; inspect result.Message for details if needed.
    Console.WriteLine($"Command executed: {result.Message}");
}
else
{
    // Command failed; handle the error using result.Message.
    Console.Error.WriteLine($"Command failed: {result.Message}");
}
```

```csharp
using System;
using System.Threading.Tasks;

var executor = new CommandExecutor();

try
{
    CommandResult result = await executor.ExecuteAsync();

    if (!result.Success)
    {
        // Treat a false Success flag as a business‑logic error.
        throw new InvalidOperationException(result.Message);
    }

    // Proceed knowing the command succeeded.
}
catch (SQLiteException ex) when (ex.Result == SQLiteErrorCode.Constraint)
{
    // Handle constraint violations specifically.
    Console.Error.WriteLine($"Constraint violation: {ex.Message}");
}
catch (ObjectDisposedException)
{
    // The executor's underlying connection was disposed before execution.
    Console.Error.WriteLine("Executor is not usable; its connection has been disposed.");
}
```

## Notes
- The `CommandExecutor` does not expose mutable public state after construction; however, its behavior depends on external resources (e.g., the SQLite connection) that may be changed or disposed by other code. Consequently, concurrent calls to `ExecuteAsync` from multiple threads are not guaranteed to be safe unless the caller ensures exclusive access or documents that the underlying resources are thread‑safe.
- If `ExecuteAsync` returns a `CommandResult` with `Success` set to `false`, the `Message` field contains the reason for failure; callers should inspect this field before attempting further operations that depend on the command's effect.
- The class does not enforce any particular command text or parameters; those must be supplied through whatever configuration mechanism the containing project uses prior to invoking `ExecuteAsync`. Failure to configure the command appropriately will result in a failed execution reflected by the `CommandResult`.
