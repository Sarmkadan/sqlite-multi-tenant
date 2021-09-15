# CommandLineParser
The `CommandLineParser` class is designed to parse command-line arguments and provide a structured way to define and handle commands, flags, and options. It allows developers to register commands, flags, and options, and then parse the command-line input to determine which command was invoked and what options or flags were provided.

## API
* `public CommandLineParser()`: The constructor for the `CommandLineParser` class, used to create a new instance.
* `public CommandLineParser RegisterCommand(string Name, string Description, Action<LegacyParsedCommand> Handler, List<string> Aliases = null)`: Registers a new command with the parser. The `Name` parameter specifies the name of the command, the `Description` parameter provides a description of the command, the `Handler` parameter specifies the action to take when the command is invoked, and the `Aliases` parameter allows for specifying alternative names for the command.
* `public CommandLineParser RegisterFlag(string Name, string Description, char? ShortName = null)`: Registers a new flag with the parser. The `Name` parameter specifies the name of the flag, the `Description` parameter provides a description of the flag, and the `ShortName` parameter allows for specifying a short name for the flag.
* `public CommandLineParser RegisterOption(string Name, string Description, char? ShortName = null, bool Required = false)`: Registers a new option with the parser. The `Name` parameter specifies the name of the option, the `Description` parameter provides a description of the option, the `ShortName` parameter allows for specifying a short name for the option, and the `Required` parameter specifies whether the option is required.
* `public LegacyParsedCommand Parse()`: Parses the command-line input and returns the parsed command.
* `public string GetHelpText()`: Returns the help text for the registered commands, flags, and options.
* `public string Name { get; }`: Gets the name of the command, flag, or option.
* `public string Description { get; }`: Gets the description of the command, flag, or option.
* `public Action<LegacyParsedCommand> Handler { get; }`: Gets the handler for the command.
* `public List<string> Aliases { get; }`: Gets the aliases for the command.
* `public Dictionary<string, OptionDefinition> Options { get; }`: Gets the options for the command.
* `public Dictionary<string, FlagDefinition> Flags { get; }`: Gets the flags for the command.
* `public char? ShortName { get; }`: Gets the short name for the flag or option.
* `public bool Required { get; }`: Gets whether the option is required.

## Usage
The following example demonstrates how to use the `CommandLineParser` class to register a command and parse the command-line input:
```csharp
var parser = new CommandLineParser();
parser.RegisterCommand("hello", "Prints a hello message", cmd => Console.WriteLine("Hello!"));
var parsedCommand = parser.Parse();
if (parsedCommand != null)
{
    parsedCommand.Handler(parsedCommand);
}
```
The following example demonstrates how to use the `CommandLineParser` class to register a flag and an option:
```csharp
var parser = new CommandLineParser();
parser.RegisterFlag("verbose", "Enables verbose mode", 'v');
parser.RegisterOption("output", "Specifies the output file", 'o', true);
var parsedCommand = parser.Parse();
if (parsedCommand != null)
{
    Console.WriteLine($"Verbose mode: {parsedCommand.Flags.ContainsKey("verbose")}");
    Console.WriteLine($"Output file: {parsedCommand.Options.ContainsKey("output")}");
}
```

## Notes
The `CommandLineParser` class is designed to be used in a single-threaded environment. If used in a multi-threaded environment, the developer must ensure that the parser is properly synchronized to avoid thread-safety issues. Additionally, the `Parse` method will throw an exception if the command-line input is invalid or if a required option is missing. The `GetHelpText` method will return a formatted string containing the help text for all registered commands, flags, and options. The `Name`, `Description`, `Handler`, `Aliases`, `Options`, and `Flags` properties are read-only and can be used to access the registered commands, flags, and options. The `ShortName` and `Required` properties are read-only and can be used to access the short name and required status of a flag or option.
