#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqliteMultiTenant.Database;

/// <summary>
/// SQLite pragmas applied to every connection a pool opens. <see cref="Validate"/> checks the
/// values against SQLite's allowed settings so a bad value fails at startup rather than on the
/// first connection. Values are case-insensitive.
/// </summary>
public sealed class SqlitePragmaOptions
{
    private static readonly string[] AllowedJournalModes = ["DELETE", "TRUNCATE", "PERSIST", "MEMORY", "WAL", "OFF"];
    private static readonly string[] AllowedSynchronousModes = ["OFF", "NORMAL", "FULL", "EXTRA"];

    /// <summary>
    /// <c>journal_mode</c>. One of <c>DELETE</c>, <c>TRUNCATE</c>, <c>PERSIST</c>, <c>MEMORY</c>,
    /// <c>WAL</c>, <c>OFF</c>. Default: <c>WAL</c>.
    /// </summary>
    public string JournalMode { get; set; } = "WAL";

    /// <summary>
    /// <c>synchronous</c>. One of <c>OFF</c>, <c>NORMAL</c>, <c>FULL</c>, <c>EXTRA</c>. Default: <c>NORMAL</c>.
    /// </summary>
    public string Synchronous { get; set; } = "NORMAL";

    /// <summary>
    /// <c>busy_timeout</c> in milliseconds. Must be zero or greater. Default: <c>5000</c>.
    /// </summary>
    public int BusyTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Whether <c>foreign_keys</c> enforcement is enabled for each connection. Default: <c>true</c>.
    /// </summary>
    public bool ForeignKeys { get; set; } = true;

    /// <summary>
    /// Validates every pragma value against its allowed set.
    /// </summary>
    /// <exception cref="OptionsValidationException">
    /// Thrown when one or more values are not allowed. The exception lists every failing setting.
    /// </exception>
    public void Validate()
    {
        var failures = new List<string>();

        if (!IsAllowed(JournalMode, AllowedJournalModes))
            failures.Add(FormatInvalid(nameof(JournalMode), JournalMode, AllowedJournalModes));
        if (!IsAllowed(Synchronous, AllowedSynchronousModes))
            failures.Add(FormatInvalid(nameof(Synchronous), Synchronous, AllowedSynchronousModes));
        if (BusyTimeoutMs < 0)
            failures.Add($"{nameof(BusyTimeoutMs)} must be zero or greater (current: {BusyTimeoutMs}).");

        if (failures.Count > 0)
            throw new OptionsValidationException(nameof(SqlitePragmaOptions), typeof(SqlitePragmaOptions), failures);
    }

    /// <summary>
    /// Applies the configured pragmas to an open connection, then reads each value back from SQLite.
    /// The returned set is what SQLite actually reports, which can differ from the request
    /// (for example, <c>journal_mode=WAL</c> reports <c>memory</c> on an in-memory database).
    /// </summary>
    /// <param name="connection">An open connection.</param>
    /// <param name="logger">Logger used to warn when SQLite does not honour a requested journal mode.</param>
    /// <returns>Pragma name to the value SQLite reports, keyed by pragma name in lower case.</returns>
    internal IReadOnlyDictionary<string, string> ApplyTo(SQLiteConnection connection, ILogger logger)
    {
        ExecuteNonQuery(connection, $"PRAGMA journal_mode = {JournalMode.ToUpperInvariant()}");
        ExecuteNonQuery(connection, $"PRAGMA synchronous = {Synchronous.ToUpperInvariant()}");
        ExecuteNonQuery(connection, $"PRAGMA busy_timeout = {BusyTimeoutMs.ToString(CultureInfo.InvariantCulture)}");
        ExecuteNonQuery(connection, $"PRAGMA foreign_keys = {(ForeignKeys ? "ON" : "OFF")}");

        var effective = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["journal_mode"] = ReadPragma(connection, "journal_mode"),
            ["synchronous"] = ReadPragma(connection, "synchronous"),
            ["busy_timeout"] = ReadPragma(connection, "busy_timeout"),
            ["foreign_keys"] = ReadPragma(connection, "foreign_keys"),
        };

        if (!string.Equals(effective["journal_mode"], JournalMode, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Requested journal_mode {Requested} but SQLite reports {Effective}",
                JournalMode.ToUpperInvariant(), effective["journal_mode"]);
        }

        return effective;
    }

    private static bool IsAllowed(string? value, string[] allowed) =>
        value is not null && Array.Exists(allowed, a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase));

    private static string FormatInvalid(string name, string? value, string[] allowed) =>
        $"{name} '{value}' is not allowed. Expected one of: {string.Join(", ", allowed)}.";

    private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string ReadPragma(SQLiteConnection connection, string name)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {name}";
        return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
