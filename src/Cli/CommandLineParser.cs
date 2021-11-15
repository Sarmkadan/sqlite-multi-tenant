#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqliteMultiTenant.Cli
{
/// <summary>
/// Advanced command-line argument parser that supports registering commands with options, flags, and aliases.
/// Provides parsing capabilities for complex command-line interfaces with validation and help text generation.
/// </summary>
public sealed class CommandLineParser
{
private readonly Dictionary<string, CommandDefinition> _commands;
private readonly List<string> _arguments;

/// <summary>
/// Initializes a new instance of the <see cref="CommandLineParser"/> class with the specified command-line arguments.
/// </summary>
/// <param name="args">The command-line arguments to parse. Can be null, in which case an empty array is used.</param>
public CommandLineParser(params string[] args)
{
_arguments = new List<string>(args ?? []);
_commands = new Dictionary<string, CommandDefinition>();
}

/// <summary>
/// Registers a command with the parser.
/// </summary>
/// <param name="name">The name of the command to register.</param>
/// <param name="description">A description of what the command does.</param>
/// <param name="handler">The action to execute when this command is invoked.</param>
/// <param name="aliases">Optional aliases for this command.</param>
/// <returns>The current <see cref="CommandLineParser"/> instance for method chaining.</returns>
/// <exception cref="ArgumentException">Thrown when the command name is null or whitespace.</exception>
public CommandLineParser RegisterCommand(string name, string description,
Action<LegacyParsedCommand> handler, params string[] aliases)
{
if (string.IsNullOrWhiteSpace(name))
throw new ArgumentException("Command name cannot be empty", nameof(name));

var command = new CommandDefinition
{
Name = name,
Description = description,
Handler = handler,
Aliases = new List<string>(aliases ?? [])
};

_commands[name] = command;

foreach (var alias in command.Aliases)
{
_commands[alias] = command;
}

return this;
}

/// <summary>
/// Registers a flag option for the last registered command.
/// </summary>
/// <param name="name">The name of the flag option.</param>
/// <param name="description">A description of what the flag does.</param>
/// <param name="shortName">Optional short name (single character) for the flag.</param>
/// <returns>The current <see cref="CommandLineParser"/> instance for method chaining.</returns>
/// <exception cref="InvalidOperationException">Thrown when no command has been registered yet.</exception>
public CommandLineParser RegisterFlag(string name, string description, char? shortName = null)
{
if (!_commands.Any())
throw new InvalidOperationException("Register a command first");

var lastCommand = _commands.Values.Last();
lastCommand.Flags[name] = new FlagDefinition
{
Name = name,
Description = description,
ShortName = shortName
};

return this;
}

/// <summary>
/// Registers a value option for the last registered command.
/// </summary>
/// <param name="name">The name of the option.</param>
/// <param name="description">A description of what the option does.</param>
/// <param name="shortName">Optional short name (single character) for the option.</param>
/// <param name="required">Whether this option is required when the command is invoked.</param>
/// <returns>The current <see cref="CommandLineParser"/> instance for method chaining.</returns>
/// <exception cref="InvalidOperationException">Thrown when no command has been registered yet.</exception>
public CommandLineParser RegisterOption(string name, string description,
char? shortName = null, bool required = false)
{
if (!_commands.Any())
throw new InvalidOperationException("Register a command first");

var lastCommand = _commands.Values.Last();
lastCommand.Options[name] = new OptionDefinition
{
Name = name,
Description = description,
ShortName = shortName,
Required = required
};

return this;
}

/// <summary>
/// Parses the registered command-line arguments and returns a parsed command object.
/// </summary>
/// <returns>A <see cref="LegacyParsedCommand"/> object containing the parsed command, options, flags, and positional arguments.
/// If parsing fails, the <see cref="LegacyParsedCommand.IsValid"/> property will be false and <see cref="LegacyParsedCommand.Error"/> will contain the error message.</returns>
public LegacyParsedCommand Parse()
{
if (_arguments.Count == 0)
return new LegacyParsedCommand { IsValid = false, Error = "No command specified" };

var commandName = _arguments[0];

if (!_commands.TryGetValue(commandName, out var commandDef))
return new LegacyParsedCommand { IsValid = false, Error = $"Unknown command: {commandName}" };

var parsed = new LegacyParsedCommand { Command = commandName };
var position = 1;

while (position < _arguments.Count)
{
var arg = _arguments[position];

if (arg.StartsWith("--"))
{
// Long option
var optionName = arg.Substring(2);
var optionDef = commandDef.Options.Values.FirstOrDefault(o =>
o.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));

if (optionDef is not null)
{
if (position + 1 < _arguments.Count && !_arguments[position + 1].StartsWith("-"))
{
position++;
parsed.Options[optionDef.Name] = _arguments[position];
}
}
}
else if (arg.StartsWith("-") && arg.Length == 2)
{
// Short option or flag
var shortName = arg[1];
var flagDef = commandDef.Flags.Values.FirstOrDefault(f => f.ShortName == shortName);
var optionDef = commandDef.Options.Values.FirstOrDefault(o => o.ShortName == shortName);

if (flagDef is not null)
{
parsed.Flags[flagDef.Name] = true;
}
else if (optionDef is not null)
{
if (position + 1 < _arguments.Count && !_arguments[position + 1].StartsWith("-"))
{
position++;
parsed.Options[optionDef.Name] = _arguments[position];
}
}
}
else
{
// Positional argument
parsed.PositionalArguments.Add(arg);
}

position++;
}

// Validate required options
foreach (var option in commandDef.Options.Values.Where(o => o.Required))
{
if (!parsed.Options.ContainsKey(option.Name))
{
parsed.IsValid = false;
parsed.Error = $"Required option missing: --{option.Name}";
return parsed;
}
}

parsed.IsValid = true;
return parsed;
}

/// <summary>
/// Generates help text for all registered commands, including their options and flags.
/// </summary>
/// <returns>A formatted string containing the help documentation for all commands.</returns>
public string GetHelpText()
{
var help = new StringBuilder();
help.AppendLine("Available commands:");
help.AppendLine();

var uniqueCommands = _commands.Values.Distinct().ToList();

foreach (var command in uniqueCommands)
{
help.Append(" ");
help.Append(command.Name);

if (command.Aliases.Any())
{
help.Append(" (");
help.Append(string.Join(", ", command.Aliases));
help.Append(")");
}

help.AppendLine();

if (!string.IsNullOrEmpty(command.Description))
{
help.Append(" ");
help.AppendLine(command.Description);
}

if (command.Options.Any() || command.Flags.Any())
{
help.AppendLine(" Options:");

foreach (var option in command.Options.Values)
{
help.Append(" --");
help.Append(option.Name);

if (option.ShortName.HasValue)
{
help.Append(" (-");
help.Append(option.ShortName);
help.Append(")");
}

if (!string.IsNullOrEmpty(option.Description))
{
help.Append(": ");
help.Append(option.Description);
}

help.AppendLine();
}

foreach (var flag in command.Flags.Values)
{
help.Append(" --");
help.Append(flag.Name);

if (flag.ShortName.HasValue)
{
help.Append(" (-");
help.Append(flag.ShortName);
help.Append(")");
}

if (!string.IsNullOrEmpty(flag.Description))
{
help.Append(": ");
help.Append(flag.Description);
}

help.AppendLine();
}
}

help.AppendLine();
}

return help.ToString();
}

private class CommandDefinition
{
public string Name { get; set; }
public string Description { get; set; }
public Action<LegacyParsedCommand> Handler { get; set; }
public List<string> Aliases { get; set; } = new List<string>();
public Dictionary<string, OptionDefinition> Options { get; set; } = new Dictionary<string, OptionDefinition>();
public Dictionary<string, FlagDefinition> Flags { get; set; } = new Dictionary<string, FlagDefinition>();
}

private class OptionDefinition
{
public string Name { get; set; }
public string Description { get; set; }
public char? ShortName { get; set; }
public bool Required { get; set; }
}

private class FlagDefinition
{
public string Name { get; set; }
public string Description { get; set; }
public char? ShortName { get; set; }
}
}

/// <summary>
/// Represents a parsed command with its options, flags, and positional arguments.
/// </summary>
public sealed class LegacyParsedCommand
{
public string Command { get; set; }
public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
public Dictionary<string, bool> Flags { get; set; } = new Dictionary<string, bool>();
public List<string> PositionalArguments { get; set; } = new List<string>();
public bool IsValid { get; set; }
public string Error { get; set; }

/// <summary>
/// Gets the value of the specified option or returns a default value if the option is not present.
/// </summary>
/// <param name="name">The name of the option to retrieve.</param>
/// <param name="defaultValue">The default value to return if the option is not found.</param>
/// <returns>The option value if found, otherwise the default value.</returns>
public string GetOption(string name, string defaultValue = null)
{
return Options.TryGetValue(name, out var value) ? value : defaultValue;
}

/// <summary>
/// Checks whether the specified flag was set in the command-line arguments.
/// </summary>
/// <param name="name">The name of the flag to check.</param>
/// <returns>True if the flag was set, otherwise false.</returns>
public bool HasFlag(string name)
{
return Flags.TryGetValue(name, out var value) && value;
}

/// <summary>
/// Gets the value of the specified option and converts it to the specified type.
/// </summary>
/// <typeparam name="T">The type to convert the option value to.</typeparam>
/// <param name="name">The name of the option to retrieve.</param>
/// <param name="defaultValue">The default value to return if the option is not found or cannot be converted.</param>
/// <returns>The converted option value if found and convertible, otherwise the default value.</returns>
public T GetOption<T>(string name, T defaultValue = default) where T : IConvertible
{
if (Options.TryGetValue(name, out var value))
{
try
{
return (T)Convert.ChangeType(value, typeof(T));
}
catch { /* Ignored */ }
}

return defaultValue;
}
}
}