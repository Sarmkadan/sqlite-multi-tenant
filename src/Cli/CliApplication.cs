// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Cli;

/// <summary>
/// Main CLI application host that orchestrates command parsing, execution, and output.
/// Provides structured error handling and user-friendly command-line interface.
/// Integrates with dependency injection to access all required services.
/// </summary>
public class CliApplication
{
    private readonly CommandParser _parser;
    private readonly CommandExecutor _executor;
    private readonly ILogger<CliApplication> _logger;
    private readonly IConsoleWriter _consoleWriter;

    public CliApplication(
        CommandParser parser,
        CommandExecutor executor,
        ILogger<CliApplication> logger,
        IConsoleWriter consoleWriter)
    {
        _parser = parser;
        _executor = executor;
        _logger = logger;
        _consoleWriter = consoleWriter;
    }

    /// <summary>
    /// Runs the CLI application with the given arguments.
    /// Parses command, executes it, and writes formatted output to console.
    /// Returns exit code (0 for success, 1 for failure).
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("=== SQLite Multi-Tenant CLI ===");

            // Parse command-line arguments
            var parsedCommand = _parser.Parse(args);

            // Execute command
            var result = await _executor.ExecuteAsync(parsedCommand);

            // Write result to console
            if (result.Success)
            {
                _consoleWriter.WriteSuccess(result.Message);
                _logger.LogInformation("Command executed successfully");
                return 0;
            }
            else
            {
                _consoleWriter.WriteError(result.Message);
                _logger.LogError($"Command failed: {result.Message}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unhandled CLI exception: {ex.Message}\n{ex.StackTrace}");
            _consoleWriter.WriteError($"Fatal error: {ex.Message}");
            return 1;
        }
    }
}

/// <summary>
/// Interface for console output with formatted messages
/// </summary>
public interface IConsoleWriter
{
    void WriteSuccess(string message);
    void WriteError(string message);
    void WriteWarning(string message);
    void WriteInfo(string message);
}

/// <summary>
/// Console output implementation with colored output support
/// </summary>
public class ConsoleWriter : IConsoleWriter
{
    public void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    public void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    public void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    public void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }
}
