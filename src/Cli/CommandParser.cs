// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Cli;

/// <summary>
/// Parses command-line arguments into structured command objects.
/// Handles validation, error messages, and help text generation.
/// </summary>
public class CommandParser
{
    private readonly Dictionary<string, CommandHandler> _commands;
    private readonly ILogger<CommandParser> _logger;

    public CommandParser(ILogger<CommandParser> logger)
    {
        _logger = logger;
        _commands = new Dictionary<string, CommandHandler>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultCommands();
    }

    // Registers all available commands with their handlers
    private void RegisterDefaultCommands()
    {
        _commands["tenant"] = new CommandHandler
        {
            Name = "tenant",
            Description = "Manage tenants",
            Subcommands = new[]
            {
                new Subcommand { Name = "create", Description = "Create a new tenant", RequiredArgs = new[] { "name" } },
                new Subcommand { Name = "list", Description = "List all tenants", RequiredArgs = Array.Empty<string>() },
                new Subcommand { Name = "get", Description = "Get tenant details", RequiredArgs = new[] { "tenantId" } },
                new Subcommand { Name = "delete", Description = "Delete a tenant", RequiredArgs = new[] { "tenantId" } },
                new Subcommand { Name = "status", Description = "Get tenant status", RequiredArgs = new[] { "tenantId" } }
            }
        };

        _commands["backup"] = new CommandHandler
        {
            Name = "backup",
            Description = "Manage backups",
            Subcommands = new[]
            {
                new Subcommand { Name = "create", Description = "Create a backup", RequiredArgs = new[] { "databaseId" } },
                new Subcommand { Name = "list", Description = "List backups", RequiredArgs = new[] { "databaseId" } },
                new Subcommand { Name = "restore", Description = "Restore from backup", RequiredArgs = new[] { "backupId", "targetPath" } },
                new Subcommand { Name = "verify", Description = "Verify backup integrity", RequiredArgs = new[] { "backupId" } },
                new Subcommand { Name = "delete", Description = "Delete a backup", RequiredArgs = new[] { "backupId" } }
            }
        };

        _commands["migration"] = new CommandHandler
        {
            Name = "migration",
            Description = "Manage database migrations",
            Subcommands = new[]
            {
                new Subcommand { Name = "pending", Description = "List pending migrations", RequiredArgs = new[] { "databaseId" } },
                new Subcommand { Name = "apply", Description = "Apply pending migrations", RequiredArgs = new[] { "databaseId" } },
                new Subcommand { Name = "rollback", Description = "Rollback last migration", RequiredArgs = new[] { "databaseId" } },
                new Subcommand { Name = "history", Description = "Show migration history", RequiredArgs = new[] { "databaseId" } }
            }
        };

        _commands["health"] = new CommandHandler
        {
            Name = "health",
            Description = "Check system health",
            Subcommands = new[]
            {
                new Subcommand { Name = "check", Description = "Run health checks", RequiredArgs = Array.Empty<string>() },
                new Subcommand { Name = "status", Description = "Get overall status", RequiredArgs = Array.Empty<string>() }
            }
        };
    }

    /// <summary>
    /// Parses command-line arguments and returns a parsed command object.
    /// Validates all required arguments and returns helpful error messages.
    /// </summary>
    public ParsedCommand Parse(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return CreateHelpCommand();

            string mainCommand = args[0].ToLower();

            if (mainCommand == "help" || mainCommand == "-h" || mainCommand == "--help")
                return CreateHelpCommand();

            if (!_commands.TryGetValue(mainCommand, out var handler))
            {
                _logger.LogWarning($"Unknown command: {mainCommand}");
                return CreateErrorCommand($"Unknown command '{mainCommand}'. Use 'help' for available commands.");
            }

            if (args.Length < 2)
                return CreateHelpCommand(mainCommand);

            string subcommand = args[1].ToLower();
            var subcommandDef = handler.Subcommands?.FirstOrDefault(s => s.Name == subcommand);

            if (subcommandDef == null)
                return CreateErrorCommand($"Unknown subcommand '{subcommand}' for '{mainCommand}'");

            var commandArgs = args.Skip(2).ToList();

            // Validate required arguments
            if (commandArgs.Count < subcommandDef.RequiredArgs.Length)
                return CreateErrorCommand(
                    $"Missing required arguments for '{mainCommand} {subcommand}'. " +
                    $"Expected: {string.Join(", ", subcommandDef.RequiredArgs)}");

            return new ParsedCommand
            {
                Success = true,
                MainCommand = mainCommand,
                Subcommand = subcommand,
                Arguments = commandArgs,
                Description = subcommandDef.Description
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Command parsing error: {ex.Message}");
            return CreateErrorCommand($"Error parsing command: {ex.Message}");
        }
    }

    private ParsedCommand CreateHelpCommand(string? command = null)
    {
        var helpText = new System.Text.StringBuilder();
        helpText.AppendLine("SQLite Multi-Tenant Manager - Command Line Interface");
        helpText.AppendLine("=====================================================\n");

        if (string.IsNullOrEmpty(command))
        {
            helpText.AppendLine("Available Commands:");
            foreach (var cmd in _commands.Values)
            {
                helpText.AppendLine($"\n  {cmd.Name}");
                helpText.AppendLine($"    {cmd.Description}");
                helpText.AppendLine("    Subcommands:");
                foreach (var sub in cmd.Subcommands ?? Array.Empty<Subcommand>())
                {
                    helpText.AppendLine($"      {sub.Name}: {sub.Description}");
                }
            }
        }
        else if (_commands.TryGetValue(command, out var handler))
        {
            helpText.AppendLine($"Command: {command}");
            helpText.AppendLine($"Description: {handler.Description}\n");
            helpText.AppendLine("Subcommands:");
            foreach (var sub in handler.Subcommands ?? Array.Empty<Subcommand>())
            {
                helpText.AppendLine($"  {command} {sub.Name}");
                helpText.AppendLine($"    {sub.Description}");
                if (sub.RequiredArgs.Length > 0)
                    helpText.AppendLine($"    Arguments: {string.Join(", ", sub.RequiredArgs)}");
            }
        }

        return new ParsedCommand
        {
            Success = true,
            MainCommand = "help",
            IsHelpCommand = true,
            Message = helpText.ToString()
        };
    }

    private ParsedCommand CreateErrorCommand(string error)
    {
        return new ParsedCommand
        {
            Success = false,
            MainCommand = "",
            Message = error,
            IsErrorCommand = true
        };
    }
}

public class CommandHandler
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Subcommand[]? Subcommands { get; set; }
}

public class Subcommand
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] RequiredArgs { get; set; } = Array.Empty<string>();
}

public class ParsedCommand
{
    public bool Success { get; set; }
    public string MainCommand { get; set; } = string.Empty;
    public string Subcommand { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsHelpCommand { get; set; }
    public bool IsErrorCommand { get; set; }
}
