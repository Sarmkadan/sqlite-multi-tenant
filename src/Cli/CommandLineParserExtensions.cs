#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SqliteMultiTenant.Cli
{
    /// <summary>
    /// Extension methods for <see cref="CommandLineParser"/> to provide additional functionality
    /// for command-line parsing scenarios.
    /// </summary>
    public static class CommandLineParserExtensions
    {
        /// <summary>
        /// Registers a command with a handler and automatically adds common flags for verbose and help output.
        /// </summary>
        /// <param name="parser">The parser instance</param>
        /// <param name="name">Command name</param>
        /// <param name="description">Command description</param>
        /// <param name="handler">Command handler</param>
        /// <param name="aliases">Command aliases</param>
        /// <returns>The parser instance for method chaining</returns>
        public static CommandLineParser RegisterCommandWithCommonFlags(
            this CommandLineParser parser,
            string name,
            string description,
            Action<LegacyParsedCommand> handler,
            params string[] aliases)
        {
            parser.RegisterCommand(name, description, handler, aliases);
            parser.RegisterFlag("verbose", "Enable verbose output");
            parser.RegisterFlag("help", "Show help message", 'h');
            return parser;
        }

        /// <summary>
        /// Checks if a specific option was provided in the parsed command.
        /// </summary>
        /// <param name="parser">The parser instance</param>
        /// <param name="parsedCommand">The parsed command</param>
        /// <param name="optionName">Option name to check</param>
        /// <returns>True if the option was provided, false otherwise</returns>
        public static bool HasOption(this CommandLineParser parser, LegacyParsedCommand parsedCommand, string optionName)
        {
            if (parsedCommand == null)
                throw new ArgumentNullException(nameof(parsedCommand));

            return parsedCommand.Options.ContainsKey(optionName);
        }

        /// <summary>
        /// Gets the number of positional arguments in the parsed command.
        /// </summary>
        /// <param name="parser">The parser instance</param>
        /// <param name="parsedCommand">The parsed command</param>
        /// <returns>Number of positional arguments</returns>
        public static int GetPositionalArgumentCount(this CommandLineParser parser, LegacyParsedCommand parsedCommand)
        {
            if (parsedCommand == null)
                throw new ArgumentNullException(nameof(parsedCommand));

            return parsedCommand.PositionalArguments.Count;
        }

        /// <summary>
        /// Gets a formatted string showing all registered commands with their descriptions.
        /// </summary>
        /// <param name="parser">The parser instance</param>
        /// <returns>Formatted string with command information</returns>
        public static string GetCommandsSummary(this CommandLineParser parser)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

            var commands = parser.GetType()
                .GetField("_commands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(parser) as Dictionary<string, object>;

            if (commands == null)
                return "No commands registered";

            var uniqueCommands = new HashSet<object>();
            var result = new System.Text.StringBuilder();
            result.AppendLine("Registered commands:");
            result.AppendLine();

            foreach (var entry in commands)
            {
                var commandDef = entry.Value.GetType()
                    .GetProperty("Value")?.GetValue(entry.Value);

                if (commandDef != null)
                {
                    var name = commandDef.GetType().GetProperty("Name")?.GetValue(commandDef) as string;
                    var description = commandDef.GetType().GetProperty("Description")?.GetValue(commandDef) as string;
                    var aliases = commandDef.GetType().GetProperty("Aliases")?.GetValue(commandDef) as List<string>;

                    if (name != null && uniqueCommands.Add(commandDef))
                    {
                        result.Append("  ");
                        result.Append(name);

                        if (aliases != null && aliases.Any())
                        {
                            result.Append(" (");
                            result.Append(string.Join(", ", aliases));
                            result.Append(")");
                        }

                        result.AppendLine();

                        if (!string.IsNullOrEmpty(description))
                        {
                            result.Append("    ");
                            result.AppendLine(description);
                        }
                        result.AppendLine();
                    }
                }
            }

            return result.ToString();
        }
    }
}