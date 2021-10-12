#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Cli;

/// <summary>
/// Provides validation helpers for <see cref="CommandParser"/> and related classes.
/// Validates command structure, required arguments, and data integrity.
/// </summary>
public static class CommandParserValidation
{
    /// <summary>
    /// Validates a <see cref="CommandParser"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The command parser to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this CommandParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CommandParser"/> is valid.
    /// </summary>
    /// <param name="value">The command parser to check</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/></returns>
    public static bool IsValid(this CommandParser value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CommandParser"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The command parser to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
    public static void EnsureValid(this CommandParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"CommandParser validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="CommandHandler"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The command handler to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this CommandHandler value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("CommandHandler.Name is null or whitespace");
        }
        else if (value.Name.Any(c => char.IsWhiteSpace(c)))
        {
            problems.Add("CommandHandler.Name contains whitespace characters");
        }

        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("CommandHandler.Description is null or whitespace");
        }

        if (value.Subcommands is not null)
        {
            var subcommandProblems = value.Subcommands.SelectMany(Validate).ToList();
            problems.AddRange(subcommandProblems);
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CommandHandler"/> is valid.
    /// </summary>
    /// <param name="value">The command handler to check</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/></returns>
    public static bool IsValid(this CommandHandler value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CommandHandler"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The command handler to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
    public static void EnsureValid(this CommandHandler value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"CommandHandler validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="Subcommand"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The subcommand to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this Subcommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Subcommand.Name is null or whitespace");
        }
        else if (value.Name.Any(c => char.IsWhiteSpace(c)))
        {
            problems.Add("Subcommand.Name contains whitespace characters");
        }

        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("Subcommand.Description is null or whitespace");
        }

        if (value.RequiredArgs is null)
        {
            problems.Add("Subcommand.RequiredArgs is null");
        }
        else
        {
            foreach (var arg in value.RequiredArgs)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    problems.Add("Subcommand.RequiredArgs contains null or whitespace entry");
                    break;
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Subcommand"/> is valid.
    /// </summary>
    /// <param name="value">The subcommand to check</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/></returns>
    public static bool IsValid(this Subcommand value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="Subcommand"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The subcommand to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
    public static void EnsureValid(this Subcommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Subcommand validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="ParsedCommand"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The parsed command to validate</param>
    /// <returns>An enumerable of validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ParsedCommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.Success)
        {
            if (string.IsNullOrWhiteSpace(value.MainCommand))
            {
                problems.Add("ParsedCommand.MainCommand is null or whitespace when Success is true");
            }

            if (value.IsHelpCommand)
            {
                // Help commands can have empty subcommand
            }
            else if (value.IsErrorCommand)
            {
                if (string.IsNullOrWhiteSpace(value.Message))
                {
                    problems.Add("ParsedCommand.Message is null or whitespace for error command");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value.Subcommand))
                {
                    problems.Add("ParsedCommand.Subcommand is null or whitespace when Success is true and not a help/error command");
                }

                if (value.Arguments is null)
                {
                    problems.Add("ParsedCommand.Arguments is null");
                }
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(value.Message))
            {
                problems.Add("ParsedCommand.Message is null or whitespace when Success is false");
            }
        }

        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("ParsedCommand.Description is null or whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ParsedCommand"/> is valid.
    /// </summary>
    /// <param name="value">The parsed command to check</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/></returns>
    public static bool IsValid(this ParsedCommand value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ParsedCommand"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The parsed command to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
    public static void EnsureValid(this ParsedCommand value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ParsedCommand validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}

