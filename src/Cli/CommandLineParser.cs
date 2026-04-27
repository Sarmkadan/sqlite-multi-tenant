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
    // Advanced command-line argument parser with support for options, flags, and subcommands
    public sealed class CommandLineParser {
        private readonly Dictionary<string, CommandDefinition> _commands;
        private readonly List<string> _arguments;

        public CommandLineParser(params string[] args)
        {
            _arguments = new List<string>(args ?? []);
            _commands = new Dictionary<string, CommandDefinition>();
        }

        // Registers a command
        public CommandLineParser RegisterCommand(string name, string description,
            Action<ParsedCommand> handler, params string[] aliases)
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

        // Registers a flag option
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

        // Registers a value option
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

        // Parses the command line arguments
        public ParsedCommand Parse()
        {
            if (_arguments.Count == 0)
                return new ParsedCommand { IsValid = false, Error = "No command specified" };

            var commandName = _arguments[0];

            if (!_commands.TryGetValue(commandName, out var commandDef))
                return new ParsedCommand { IsValid = false, Error = $"Unknown command: {commandName}" };

            var parsed = new ParsedCommand { Command = commandName };
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

        // Gets help text for all commands
        public string GetHelpText()
        {
            var help = new StringBuilder();
            help.AppendLine("Available commands:");
            help.AppendLine();

            var uniqueCommands = _commands.Values.Distinct().ToList();

            foreach (var command in uniqueCommands)
            {
                help.Append("  ");
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
                    help.Append("    ");
                    help.AppendLine(command.Description);
                }

                if (command.Options.Any() || command.Flags.Any())
                {
                    help.AppendLine("    Options:");

                    foreach (var option in command.Options.Values)
                    {
                        help.Append("      --");
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
                        help.Append("      --");
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
            public Action<ParsedCommand> Handler { get; set; }
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

    public sealed class ParsedCommand {
        public string Command { get; set; }
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> Flags { get; set; } = new Dictionary<string, bool>();
        public List<string> PositionalArguments { get; set; } = new List<string>();
        public bool IsValid { get; set; }
        public string Error { get; set; }

        public string GetOption(string name, string defaultValue = null)
        {
            return Options.TryGetValue(name, out var value) ? value : defaultValue;
        }

        public bool HasFlag(string name)
        {
            return Flags.TryGetValue(name, out var value) && value;
        }

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
