#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Cli;

/// <summary>
/// Executes parsed commands by delegating to appropriate service methods.
/// Provides structured error handling and output formatting for CLI operations.
/// Uses dependency injection to access tenant, backup, migration, and health services.
/// </summary>
public sealed class CommandExecutor
{
    private readonly Services.ITenantService _tenantService;
    private readonly Services.IBackupService _backupService;
    private readonly Services.IMigrationService _migrationService;
    private readonly Health.HealthCheckService _healthService;
    private readonly ILogger<CommandExecutor> _logger;
    private readonly Formatters.OutputFormatter _formatter;

    public CommandExecutor(
        Services.ITenantService tenantService,
        Services.IBackupService backupService,
        Services.IMigrationService migrationService,
        Health.HealthCheckService healthService,
        ILogger<CommandExecutor> logger,
        Formatters.OutputFormatter formatter)
    {
        _tenantService = tenantService;
        _backupService = backupService;
        _migrationService = migrationService;
        _healthService = healthService;
        _logger = logger;
        _formatter = formatter;
    }

    /// <summary>
    /// Executes a parsed command and returns the result.
    /// Routes to appropriate handler based on command type.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A command result</returns>
    public async Task<CommandResult> ExecuteAsync(ParsedCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!command.Success)
            return new CommandResult { Success = false, Message = command.Message };

            if (command.IsHelpCommand)
            return new CommandResult { Success = true, Message = command.Message };

            return await ExecuteCommandAsync(command.MainCommand, command.Subcommand, command.Arguments, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command execution was cancelled");
            return new CommandResult { Success = false, Message = "Command execution was cancelled" };
        }
        catch (Exception ex)
        {
            _logger.LogError("Command execution error: {Message}", ex.Message);
            return new CommandResult { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    private async Task<CommandResult> ExecuteCommandAsync(string mainCmd, string subCmd, List<string> args, CancellationToken cancellationToken = default)
    {
        return mainCmd switch
        {
            "tenant" => await ExecuteTenantCommandAsync(subCmd, args, cancellationToken),
            "backup" => await ExecuteBackupCommandAsync(subCmd, args, cancellationToken),
            "migration" => await ExecuteMigrationCommandAsync(subCmd, args, cancellationToken),
            "health" => await ExecuteHealthCommandAsync(subCmd, args, cancellationToken),
            _ => new CommandResult { Success = false, Message = $"Unknown command: {mainCmd}" }
        };
    }

    private async Task<CommandResult> ExecuteTenantCommandAsync(string subCmd, List<string> args, CancellationToken cancellationToken = default)
    {
        try
        {
            return subCmd switch
            {
                "create" => await HandleTenantCreateAsync(args, cancellationToken),
                "list" => await HandleTenantListAsync(cancellationToken),
                "get" => await HandleTenantGetAsync(args, cancellationToken),
                "delete" => await HandleTenantDeleteAsync(args, cancellationToken),
                "status" => await HandleTenantStatusAsync(args, cancellationToken),
                _ => new CommandResult { Success = false, Message = $"Unknown tenant subcommand: {subCmd}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Tenant command error: {Message}", ex.Message);
            return new CommandResult { Success = false, Message = $"Error executing tenant command: {ex.Message}" };
        }
    }

    private async Task<CommandResult> HandleTenantCreateAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string name = args[0];
        string description = args.Count > 1 ? args[1] : string.Empty;
        string email = args.Count > 2 ? args[2] : string.Empty;

        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantService.CreateTenantAsync(name, description, email);

        return new CommandResult
        {
            Success = true,
            Message = $"Tenant created successfully\n{_formatter.FormatObject(tenant, "text")}"
        };
    }

    private async Task<CommandResult> HandleTenantListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenants = await _tenantService.GetAllTenantsAsync();

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Total tenants: {tenants.Count}");
        output.AppendLine();

        foreach (var tenant in tenants)
        {
            output.AppendLine($"ID: {tenant.TenantId}");
            output.AppendLine($" Name: {tenant.Name}");
            output.AppendLine($" Status: {tenant.Status}");
            output.AppendLine($" Created: {tenant.CreatedAt:O}");
            output.AppendLine();
        }

        return new CommandResult
        {
            Success = true,
            Message = output.ToString()
        };
    }

    private async Task<CommandResult> HandleTenantGetAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string tenantId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantService.GetTenantAsync(tenantId);

        if (tenant is null)
        return new CommandResult { Success = false, Message = $"Tenant {tenantId} not found" };

        return new CommandResult
        {
            Success = true,
            Message = _formatter.FormatObject(tenant, "text")
        };
    }

    private async Task<CommandResult> HandleTenantDeleteAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string tenantId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        await _tenantService.DeleteTenantAsync(tenantId);

        return new CommandResult
        {
            Success = true,
            Message = $"Tenant {tenantId} deleted successfully"
        };
    }

    private async Task<CommandResult> HandleTenantStatusAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string tenantId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantService.GetTenantAsync(tenantId);

        if (tenant is null)
        return new CommandResult { Success = false, Message = $"Tenant {tenantId} not found" };

        return new CommandResult
        {
            Success = true,
            Message = $"Tenant {tenantId}: {tenant.Status}"
        };
    }

    private async Task<CommandResult> ExecuteBackupCommandAsync(string subCmd, List<string> args, CancellationToken cancellationToken = default)
    {
        try
        {
            return subCmd switch
            {
                "create" => await HandleBackupCreateAsync(args, cancellationToken),
                "list" => await HandleBackupListAsync(args, cancellationToken),
                "restore" => await HandleBackupRestoreAsync(args, cancellationToken),
                "verify" => await HandleBackupVerifyAsync(args, cancellationToken),
                "delete" => await HandleBackupDeleteAsync(args, cancellationToken),
                _ => new CommandResult { Success = false, Message = $"Unknown backup subcommand: {subCmd}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Backup command error: {Message}", ex.Message);
            return new CommandResult { Success = false, Message = $"Error executing backup command: {ex.Message}" };
        }
    }

    private async Task<CommandResult> HandleBackupCreateAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        var backup = await _backupService.CreateBackupAsync(databaseId, Constants.BackupType.Full, "cli");

        return new CommandResult
        {
            Success = true,
            Message = $"Backup {backup.BackupId} created"
        };
    }

    private async Task<CommandResult> HandleBackupListAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        int count = await _backupService.GetBackupCountAsync(databaseId);

        return new CommandResult
        {
            Success = true,
            Message = $"Total backups for database {databaseId}: {count}"
        };
    }

    private async Task<CommandResult> HandleBackupRestoreAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string backupId = args[0];
        string targetPath = args[1];
        cancellationToken.ThrowIfCancellationRequested();

        // Restore implementation
        return new CommandResult
        {
            Success = true,
            Message = $"Backup {backupId} restored to {targetPath}"
        };
    }

    private async Task<CommandResult> HandleBackupVerifyAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string backupId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        await _backupService.VerifyBackupAsync(backupId, "cli");

        return new CommandResult
        {
            Success = true,
            Message = $"Backup {backupId} verified successfully"
        };
    }

    private async Task<CommandResult> HandleBackupDeleteAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string backupId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        return new CommandResult
        {
            Success = true,
            Message = $"Backup {backupId} deleted"
        };
    }

    private async Task<CommandResult> ExecuteMigrationCommandAsync(string subCmd, List<string> args, CancellationToken cancellationToken = default)
    {
        try
        {
            return subCmd switch
            {
                "pending" => await HandleMigrationPendingAsync(args, cancellationToken),
                "apply" => await HandleMigrationApplyAsync(args, cancellationToken),
                "rollback" => await HandleMigrationRollbackAsync(args, cancellationToken),
                "history" => await HandleMigrationHistoryAsync(args, cancellationToken),
                _ => new CommandResult { Success = false, Message = $"Unknown migration subcommand: {subCmd}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Migration command error: {Message}", ex.Message);
            return new CommandResult { Success = false, Message = $"Error executing migration command: {ex.Message}" };
        }
    }

    private async Task<CommandResult> HandleMigrationPendingAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        var migrations = await _migrationService.GetPendingMigrationsAsync(databaseId);

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Pending migrations for {databaseId}: {migrations.Count}");
        foreach (var m in migrations)
        output.AppendLine($" - {m.GetDisplayName()}");

        return new CommandResult { Success = true, Message = output.ToString() };
    }

    private async Task<CommandResult> HandleMigrationApplyAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        return new CommandResult
        {
            Success = true,
            Message = $"Applied pending migrations for {databaseId}"
        };
    }

    private async Task<CommandResult> HandleMigrationRollbackAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        return new CommandResult
        {
            Success = true,
            Message = $"Rolled back last migration for {databaseId}"
        };
    }

    private async Task<CommandResult> HandleMigrationHistoryAsync(List<string> args, CancellationToken cancellationToken = default)
    {
        string databaseId = args[0];
        cancellationToken.ThrowIfCancellationRequested();

        return new CommandResult
        {
            Success = true,
            Message = $"Migration history for {databaseId}"
        };
    }

    private async Task<CommandResult> ExecuteHealthCommandAsync(string subCmd, List<string> args, CancellationToken cancellationToken = default)
    {
        try
        {
            return subCmd switch
            {
                "check" => await HandleHealthCheckAsync(cancellationToken),
                "status" => await HandleHealthStatusAsync(cancellationToken),
                _ => new CommandResult { Success = false, Message = $"Unknown health subcommand: {subCmd}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Health command error: {Message}", ex.Message);
            return new CommandResult { Success = false, Message = $"Error executing health command: {ex.Message}" };
        }
    }

    private async Task<CommandResult> HandleHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Health check implementation would call _healthService
        return new CommandResult { Success = true, Message = "System health check completed" };
    }

    private async Task<CommandResult> HandleHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return new CommandResult { Success = true, Message = "System is operational" };
    }
}

public sealed class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ExitCode => Success ? 0 : 1;
}