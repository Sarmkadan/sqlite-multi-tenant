# CommandLineParserExtensions

`CommandLineParserExtensions` is a static helper class providing extension methods for the `CommandLineParser` type within the `SqliteMultiTenant.Cli` namespace. These methods streamline command registration, option checking, and argument validation to simplify the development of command-line interfaces for multi-tenant SQLite database management.

## API

### RegisterCommandWithCommonFlags
Registers a new command to the parser, automatically configuring it with common, standard flags.

*   **Parameters:**
    *   `parser` (`CommandLineParser`): The parser instance to extend.
    *   `commandName` (`string`): The name of the command.
    *   `description` (`string`): A brief description of the command's purpose.
    *   `action` (`Action<LegacyParsedCommand>`): The callback to execute when the command is invoked.
    *   `aliases` (`params string[]`): Optional array of aliases for the command.
*   **Returns:**
    *   The `CommandLineParser` instance, enabling fluent method chaining.
*   **Throws:**
    *   `ArgumentNullException` if `parser` or `commandName` is null.

### HasOption
Determines whether a specified option has been provided by the user in the command-line arguments.

*   **Parameters:**
    *   `parser` (`CommandLineParser`): The parser instance.
    *   `optionName` (`string`): The name of the option to check.
*   **Returns:**
    *   `true` if the option was parsed successfully; otherwise, `false`.

### GetPositionalArgumentCount
Retrieves the total number of positional arguments provided by the user, excluding flags and options.

*   **Parameters:**
    *   `parser` (`CommandLineParser`): The parser instance.
*   **Returns:**
    *   An `int` representing the count of positional arguments.

### GetCommandsSummary
Generates a formatted summary string of all registered commands, typically used for help output or usage documentation.

*   **Parameters:**
    *   `parser` (`CommandLineParser`): The parser instance.
*   **Returns:**
    *   A `string` containing the summary of available commands.

## Usage

```csharp
// Example 1: Registering a command
var parser = new CommandLineParser();
parser.RegisterCommandWithCommonFlags(
    "tenant-list",
    "Lists all registered tenants.",
    cmd => { /* Logic to list tenants */ },
    "ls-tenants", "list"
);

// Example 2: Checking options and arguments
if (parser.HasOption("--verbose"))
{
    Console.WriteLine("Verbose mode enabled.");
}

int argCount = parser.GetPositionalArgumentCount();
Console.WriteLine($"Number of positional arguments: {argCount}");
```

## Notes

*   **Thread Safety:** These extension methods are designed to be thread-safe when accessing the internal state of the `CommandLineParser` instance, assuming the `CommandLineParser` itself is initialized correctly and not concurrently modified during parsing operations.
*   **Edge Cases:** If `RegisterCommandWithCommonFlags` is called with a `commandName` that already exists within the parser, the behavior is implementation-dependent, typically either throwing an exception or overwriting the existing command configuration.
*   **Fluent Interface:** `RegisterCommandWithCommonFlags` supports fluent chaining, allowing for multiple commands to be registered in a single statement.
