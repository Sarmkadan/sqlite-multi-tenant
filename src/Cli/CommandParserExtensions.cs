using System;
using System.Collections.Generic;
using System.Linq;

namespace SqliteMultiTenant.Cli
{
	/// <summary>
	/// Provides extension methods for the <see cref="CommandParser"/> class.
	/// </summary>
	public static class CommandParserExtensions
	{
		/// <summary>
		/// Validates that the parsed command's arguments satisfy the required arguments for the specified subcommand.
		/// </summary>
		/// <param name="parser">The command parser instance.</param>
		/// <param name="parsedCommand">The parsed command to validate.</param>
		/// <param name="commandHandler">The command handler containing subcommand definitions.</param>
		/// <returns>A list of validation errors, or an empty list if valid.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/>, <paramref name="parsedCommand"/>, or <paramref name="commandHandler"/> is null.</exception>
		public static IReadOnlyList<string> ValidateSubcommandArguments(
			this CommandParser parser,
			ParsedCommand parsedCommand,
			CommandHandler commandHandler)
		{
			ArgumentNullException.ThrowIfNull(parser);
			ArgumentNullException.ThrowIfNull(parsedCommand);
			ArgumentNullException.ThrowIfNull(commandHandler);

			var errors = new List<string>();

			if (string.IsNullOrEmpty(parsedCommand.Subcommand))
				return errors;

			var subcommand = commandHandler.Subcommands?
				.FirstOrDefault(s => s?.Name.Equals(parsedCommand.Subcommand, StringComparison.OrdinalIgnoreCase) ?? false);

			if (subcommand is null)
				return errors;

			var requiredArgs = subcommand.RequiredArgs;
			if (requiredArgs.Length == 0)
				return errors;

			var missingArgs = requiredArgs
				.Where(arg => !parsedCommand.Arguments.Contains(arg, StringComparer.OrdinalIgnoreCase))
				.ToList();

			if (missingArgs.Count > 0)
			{
				errors.AddRange(missingArgs.Select(arg =>
					$"Missing required argument: {arg} for subcommand '{subcommand.Name}'"));
			}

			return errors.AsReadOnly();
		}

		/// <summary>
		/// Determines if the specified subcommand exists in the command handler.
		/// </summary>
		/// <param name="parser">The command parser instance.</param>
		/// <param name="commandHandler">The command handler containing subcommand definitions.</param>
		/// <param name="subcommandName">The name of the subcommand to check.</param>
		/// <returns>True if the subcommand exists; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/>, <paramref name="commandHandler"/>, or <paramref name="subcommandName"/> is null or empty.</exception>
		public static bool HasSubcommand(
			this CommandParser parser,
			CommandHandler commandHandler,
			string subcommandName)
		{
			ArgumentNullException.ThrowIfNull(parser);
			ArgumentNullException.ThrowIfNull(commandHandler);
			ArgumentException.ThrowIfNullOrEmpty(subcommandName);

			return commandHandler.Subcommands?.Any(s => s.Name.Equals(subcommandName, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		/// <summary>
		/// Generates a formatted help message for the specified command handler.
		/// </summary>
		/// <param name="parser">The command parser instance.</param>
		/// <param name="commandHandler">The command handler to generate help for.</param>
		/// <returns>A formatted help message string.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> or <paramref name="commandHandler"/> is null.</exception>
		public static string GenerateHelpText(
			this CommandParser parser,
			CommandHandler commandHandler)
		{
			ArgumentNullException.ThrowIfNull(parser);
			ArgumentNullException.ThrowIfNull(commandHandler);

			var help = new System.Text.StringBuilder();
			help.AppendLine($"Usage: {commandHandler.Name} [options]");
			help.AppendLine();
			help.AppendLine($"Description: {commandHandler.Description}");

			if (commandHandler.Subcommands is not null && commandHandler.Subcommands.Length > 0)
			{
				help.AppendLine();
				help.AppendLine("Available subcommands:");
				foreach (var subcommand in commandHandler.Subcommands)
				{
					help.AppendLine($" {subcommand.Name,-15} {subcommand.Description}");
					if (subcommand.RequiredArgs.Length > 0)
					{
						help.AppendLine($" Required arguments: {string.Join(", ", subcommand.RequiredArgs)}");
					}
			}
		}

			return help.ToString();
		}
	}
}