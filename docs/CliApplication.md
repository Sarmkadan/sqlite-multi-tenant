# CliApplication

`CliApplication` is the entry-point class for the `sqlite-multi-tenant` command-line interface. It encapsulates application initialization, argument parsing, and the top-level execution flow. The class exposes a single async entry method, `RunAsync`, which returns a process exit code, and relies on a nested `ConsoleWriter` for all structured console output.

## API

### `CliApplication`

```csharp
public CliApplication()
```

Default constructor. Creates a new instance of the CLI application. No configuration or dependency injection is performed at this stage; any required setup occurs inside `RunAsync`.

- **Parameters:** None.
- **Return value:** A new `CliApplication` instance.
- **Exceptions:** None thrown from the constructor itself.

---

### `RunAsync`

```csharp
public async Task<int> RunAsync()
```

Executes the application logic asynchronously. This method is expected to parse command-line arguments, dispatch to the appropriate command handlers, and manage the overall lifecycle of a single CLI invocation.

- **Parameters:** None (arguments are typically obtained from the environment or a static context).
- **Return value:** A `Task<int>` representing the asynchronous operation. The resulting integer is the process exit code: zero indicates success, non-zero indicates an error or abnormal termination.
- **Exceptions:** Exceptions thrown during argument parsing or command execution may propagate out of the returned task. Callers should await the task within a try-catch block if graceful handling is required.

---

### `ConsoleWriter` (nested sealed class)

```csharp
public sealed class ConsoleWriter : IConsoleWriter
```

A nested sealed class responsible for writing color-coded, categorized messages to the standard output and standard error streams. It implements `IConsoleWriter`, which defines the contract for the four output methods below.

- **Inheritance:** Implements `IConsoleWriter`.
- **Instantiation:** Instances are created internally by `CliApplication`; the class is not intended for external construction.

---

### `ConsoleWriter.WriteSuccess`

```csharp
public void WriteSuccess(string message)
```

Writes a success message to the console. Typically rendered in a green or otherwise positive-indicating color, depending on implementation.

- **Parameters:**
  - `message` (`string`): The success message to display. Must not be `null`; behavior with `null` is undefined and may throw `ArgumentNullException`.
- **Return value:** None.
- **Exceptions:** May throw `ArgumentNullException` if `message` is `null`. May throw `IOException` if the underlying output stream is closed or unavailable.

---

### `ConsoleWriter.WriteError`

```csharp
public void WriteError(string message)
```

Writes an error message to the standard error stream. Typically rendered in red or another alert color.

- **Parameters:**
  - `message` (`string`): The error message to display. Must not be `null`.
- **Return value:** None.
- **Exceptions:** May throw `ArgumentNullException` if `message` is `null`. May throw `IOException` if the error stream is unavailable.

---

### `ConsoleWriter.WriteWarning`

```csharp
public void WriteWarning(string message)
```

Writes a warning message to the console. Typically rendered in yellow or an equivalent cautionary color.

- **Parameters:**
  - `message` (`string`): The warning message to display. Must not be `null`.
- **Return value:** None.
- **Exceptions:** May throw `ArgumentNullException` if `message` is `null`. May throw `IOException` on stream failure.

---

### `ConsoleWriter.WriteInfo`

```csharp
public void WriteInfo(string message)
```

Writes an informational message to the console. Rendered in the default console color or a neutral style.

- **Parameters:**
  - `message` (`string`): The informational message to display. Must not be `null`.
- **Return value:** None.
- **Exceptions:** May throw `ArgumentNullException` if `message` is `null`. May throw `IOException` on stream failure.

## Usage

### Example 1: Basic invocation with exit-code handling

```csharp
using System;
using System.Threading.Tasks;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CliApplication();
        int exitCode = await app.RunAsync();
        return exitCode;
    }
}
```

This example demonstrates the minimal boilerplate required to run the CLI. The `Main` method instantiates `CliApplication`, awaits `RunAsync`, and propagates the exit code to the operating system.

### Example 2: Invocation with exception guarding and custom output

```csharp
using System;
using System.Threading.Tasks;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var app = new CliApplication();
            return await app.RunAsync();
        }
        catch (Exception ex)
        {
            // Fallback error output if the application itself cannot write to console
            await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
            return 1;
        }
    }
}
```

This example wraps the call in a try-catch block to ensure that even if `RunAsync` throws an unhandled exception before the `ConsoleWriter` is initialized, a non-zero exit code is returned and the error is reported on the standard error stream.

## Notes

- **Thread safety:** `CliApplication` is not designed for concurrent use. A single instance should be created and `RunAsync` called once per process. The nested `ConsoleWriter` writes to shared static streams (`Console.Out`, `Console.Error`), which are inherently thread-safe for individual write operations, but interleaving may occur if multiple threads write simultaneously. The intended usage pattern is single-threaded output from within `RunAsync`.
- **Edge cases:**
  - If `RunAsync` is invoked multiple times on the same instance, behavior is undefined and may result in duplicated output, state corruption, or exceptions.
  - The `ConsoleWriter` methods assume the console streams are open. In headless environments or when output is redirected to a closed pipe, `IOException` may be thrown. Callers relying on long-running or daemonized processes should consider this possibility.
  - All `ConsoleWriter` methods require non-null `message` arguments. Passing `null` will likely result in an `ArgumentNullException`, though the exact exception type depends on the implementation of `IConsoleWriter`.
  - The exit code returned by `RunAsync` follows the convention of zero for success and non-zero for failure. Specific non-zero values may correspond to distinct error categories as defined by the application's command handlers.
