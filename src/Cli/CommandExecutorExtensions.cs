#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Cli;

/// <summary>
/// Provides extension methods for <see cref="CommandExecutor"/> to simplify common CLI operations
/// and improve code readability when working with command execution.
/// </summary>
public static class CommandExecutorExtensions
{
    /// <summary>
    /// Executes a command with automatic error handling and returns a formatted result.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="command">The command to execute</param>
    /// <param name="successMessage">Optional success message to override default</param>
    /// <returns>A command result with success status and message</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> or <paramref name="command"/> is null</exception>
    public static async Task<CommandResult> ExecuteWithSuccessMessageAsync(
        this CommandExecutor executor,
        ParsedCommand command,
        string? successMessage = null)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(command);

        var result = await executor.ExecuteAsync(command);

        if (result.Success && successMessage != null)
        {
            result.Message = successMessage;
        }

        return result;
    }

    /// <summary>
    /// Executes a tenant create command with pre-formatted success message.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="name">Tenant name</param>
    /// <param name="description">Optional tenant description</param>
    /// <param name="email">Optional contact email</param>
    /// <returns>A command result with tenant creation status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> is null or <paramref name="name"/> is null or whitespace</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or whitespace</exception>
    public static async Task<CommandResult> CreateTenantAsync(
        this CommandExecutor executor,
        string name,
        string? description = null,
        string? email = null)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        var command = new ParsedCommand
        {
            Success = true,
            MainCommand = "tenant",
            Subcommand = "create",
            Arguments = new List<string> { name }
        };

        if (description != null)
        {
            command.Arguments.Add(description);
        }

        if (email != null)
        {
            command.Arguments.Add(email);
        }

        return await executor.ExecuteAsync(command);
    }

    /// <summary>
    /// Executes a tenant list command and returns formatted tenant information.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <returns>A command result with formatted tenant list</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> is null</exception>
    public static async Task<CommandResult> ListTenantsAsync(this CommandExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        var command = new ParsedCommand
        {
            Success = true,
            MainCommand = "tenant",
            Subcommand = "list",
            Arguments = new List<string>()
        };

        return await executor.ExecuteAsync(command);
    }

    /// <summary>
    /// Executes a backup create command for the specified database.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="databaseId">The database identifier</param>
    /// <returns>A command result with backup creation status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> or <paramref name="databaseId"/> is null or whitespace</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseId"/> is null or whitespace</exception>
    public static async Task<CommandResult> CreateBackupAsync(
        this CommandExecutor executor,
        string databaseId)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(databaseId, nameof(databaseId));

        var command = new ParsedCommand
        {
            Success = true,
            MainCommand = "backup",
            Subcommand = "create",
            Arguments = new List<string> { databaseId }
        };

        return await executor.ExecuteAsync(command);
    }

    /// <summary>
    /// Executes a migration pending command to check for pending migrations.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="databaseId">The database identifier</param>
    /// <returns>A command result with pending migrations information</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> or <paramref name="databaseId"/> is null or whitespace</exception>
    /// <exception cref="ArgumentException"><paramref name="databaseId"/> is null or whitespace</exception>
    public static async Task<CommandResult> CheckPendingMigrationsAsync(
        this CommandExecutor executor,
        string databaseId)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrEmpty(databaseId, nameof(databaseId));

        var command = new ParsedCommand
        {
            Success = true,
            MainCommand = "migration",
            Subcommand = "pending",
            Arguments = new List<string> { databaseId }
        };

        return await executor.ExecuteAsync(command);
    }

    /// <summary>
    /// Executes a health check command and returns system status.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <returns>A command result with health check status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> is null</exception>
    public static async Task<CommandResult> CheckHealthAsync(this CommandExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        var command = new ParsedCommand
        {
            Success = true,
            MainCommand = "health",
            Subcommand = "check",
            Arguments = new List<string>()
        };

        return await executor.ExecuteAsync(command);
    }

    /// <summary>
    /// Executes a command and ensures the result is successful, throwing if not.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="command">The command to execute</param>
    /// <returns>The command result</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> or <paramref name="command"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when command execution fails</exception>
    public static async Task<CommandResult> ExecuteOrThrowAsync(this CommandExecutor executor, ParsedCommand command)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(command);

        var result = await executor.ExecuteAsync(command);

        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }

        return result;
    }

    /// <summary>
    /// Executes a command with a custom timeout.
    /// </summary>
    /// <param name="executor">The command executor instance</param>
    /// <param name="command">The command to execute</param>
    /// <param name="timeoutSeconds">Timeout in seconds</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A command result</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> or <paramref name="command"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeoutSeconds"/> is less than or equal to zero</exception>
    public static async Task<CommandResult> ExecuteWithTimeoutAsync(
        this CommandExecutor executor,
        ParsedCommand command,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(command);
        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be greater than zero");
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        return await executor.ExecuteAsync(command, linkedCts.Token);
    }
}