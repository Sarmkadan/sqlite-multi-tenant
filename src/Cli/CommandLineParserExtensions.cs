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
        /// <param name="parser">The parser instance.</param>
        /// <param name="name">Command name.</param>
        /// <param name="description">Command description.</param>
        /// <param name="handler">Command handler.</param>
        /// <param name="aliases">Command aliases.</param>
        /// <returns>The parser instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/>, <paramref name="name"/>, or <paramref name="handler"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
        public static CommandLineParser RegisterCommandWithCommonFlags(
            this CommandLineParser parser,
            string name,
            string description,
            Action<LegacyParsedCommand> handler,
            params string[] aliases)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(handler);

            parser.RegisterCommand(name, description, handler, aliases);
            parser.RegisterFlag("verbose", "Enable verbose output");
            parser.RegisterFlag("help", "Show help message", 'h');
            return parser;
        }

        /// <summary>
        /// Checks if a specific option was provided in the parsed command.
        /// </summary>
        /// <param name="parsedCommand">The parsed command.</param>
        /// <param name="optionName">Option name to check.</param>
        /// <returns>True if the option was provided; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsedCommand"/> or <paramref name="optionName"/> is null.</exception>
        public static bool HasOption(this CommandLineParser _, LegacyParsedCommand parsedCommand, string optionName)
        {
            ArgumentNullException.ThrowIfNull(parsedCommand);
            ArgumentNullException.ThrowIfNull(optionName);

            return parsedCommand.Options.ContainsKey(optionName);
        }

        /// <summary>
        /// Gets the number of positional arguments in the parsed command.
        /// </summary>
        /// <param name="parsedCommand">The parsed command.</param>
        /// <returns>Number of positional arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsedCommand"/> is null.</exception>
        public static int GetPositionalArgumentCount(this CommandLineParser _, LegacyParsedCommand parsedCommand)
        {
            ArgumentNullException.ThrowIfNull(parsedCommand);

            return parsedCommand.PositionalArguments.Count;
        }

        /// <summary>
        /// Gets a formatted string showing all registered commands with their descriptions.
        /// </summary>
        /// <param name="parser">The parser instance.</param>
        /// <returns>Formatted string with command information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is null.</exception>
        public static string GetCommandsSummary(this CommandLineParser parser)
        {
            ArgumentNullException.ThrowIfNull(parser);

            var commands = parser.GetCommands();

            if (commands.Count == 0)
            {
                return "No commands registered";
            }

            var result = new System.Text.StringBuilder();
            result.AppendLine("Registered commands:");
            result.AppendLine();

            foreach (var command in commands)
            {
                result.Append(" ");
                result.Append(command.Name);

                if (command.Aliases.Count > 0)
                {
                    result.Append(" (");
                    result.Append(string.Join(", ", command.Aliases));
                    result.Append(")");
                }

                result.AppendLine();

                if (!string.IsNullOrEmpty(command.Description))
                {
                    result.Append(" ");
                    result.AppendLine(command.Description);
                }

                result.AppendLine();
            }

            return result.ToString();
        }
    }
}
