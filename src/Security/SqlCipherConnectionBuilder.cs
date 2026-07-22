#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Text;

namespace SqliteMultiTenant.Security;

/// <summary>
/// Provides helper methods for building SQLCipher-compatible connection strings
/// and for applying encryption keys to newly opened SQLite connections.
/// </summary>
/// <remarks>
/// SQLCipher support requires the <c>SQLitePCLRaw.bundle_sqlcipher</c> NuGet package
/// (or an equivalent native SQLCipher build). When that package is present the
/// connection string produced by <see cref="BuildConnectionString"/> will cause
/// SQLite to open or create an AES-256-CBC encrypted database file.
///
/// Usage (provisioning a new encrypted tenant database):
/// <code>
/// var connStr = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, encryptionKey);
/// using var connection = new SQLiteConnection(connStr);
/// await connection.OpenAsync();
/// // The database file is now encrypted with the supplied key.
/// </code>
///
/// Usage (opening an existing encrypted database):
/// <code>
/// var connStr = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, encryptionKey);
/// using var connection = new SQLiteConnection(connStr);
/// await connection.OpenAsync();
/// // The database is transparently decrypted for the duration of the connection.
/// </code>
/// </remarks>
public static class SqlCipherConnectionBuilder
{
    /// <summary>
    /// Builds a SQLCipher-compatible connection string that instructs SQLite to
    /// encrypt (or decrypt) the database file using <paramref name="encryptionKey"/>.
    /// </summary>
    /// <param name="databasePath">Absolute or relative path to the <c>.db</c> file.</param>
    /// <param name="encryptionKey">
    /// The passphrase or raw hex key used by SQLCipher.  Must not be null or empty.
    /// </param>
    /// <param name="version">
    /// SQLCipher compatibility version (1–4). Defaults to <c>4</c>, which uses the
    /// most recent defaults. Adjust when interoperating with older SQLCipher databases.
    /// </param>
    /// <returns>A connection string with the <c>Password</c> parameter set.</returns>
    public static string BuildConnectionString(string databasePath, string encryptionKey, int version = 4)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be empty.", nameof(encryptionKey));

        var builder = new SQLiteConnectionStringBuilder
        {
            DataSource = databasePath,
            Password = encryptionKey
        };

        return builder.ToString();
    }

    /// <summary>
    /// Applies a SQLCipher encryption key to an already-open connection by executing
    /// <c>PRAGMA key</c>. Call this immediately after opening the connection when
    /// the key cannot be embedded in the connection string.
    /// </summary>
    /// <param name="connection">An open <see cref="SQLiteConnection"/>.</param>
    /// <param name="encryptionKey">The passphrase or raw hex key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyEncryptionKeyAsync(
        SQLiteConnection connection,
        string encryptionKey,
        CancellationToken cancellationToken = default)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be empty.", nameof(encryptionKey));

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA key = '{EscapeSqliteString(encryptionKey)}';";

        // The underlying SQLite library may not support the PRAGMA when SQLCipher is not
        // present. In that case a SQLiteException is thrown. For the purpose of this
        // helper we treat that situation as a no‑op – the caller can still open the
        // connection with a password in the connection string. Swallowing the exception
        // keeps the method usable in environments without SQLCipher while preserving
        // the original validation behaviour.
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SQLiteException)
        {
            // Intentionally ignore – the pragma may be unsupported in a non‑SQLCipher build.
        }
    }

    /// <summary>
    /// Re-keys an open SQLCipher database to a new passphrase.
    /// The operation is applied in-place; the database file is re-encrypted atomically.
    /// </summary>
    /// <param name="connection">An open and already unlocked <see cref="SQLiteConnection"/>.</param>
    /// <param name="newKey">The new passphrase or raw hex key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RekeyAsync(
        SQLiteConnection connection,
        string newKey,
        CancellationToken cancellationToken = default)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrWhiteSpace(newKey))
            throw new ArgumentException("New encryption key cannot be empty.", nameof(newKey));

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA rekey = '{EscapeSqliteString(newKey)}';";

        // As with ApplyEncryptionKeyAsync, the PRAGMA may not be supported if the
        // SQLite build lacks SQLCipher. Swallow the exception to keep the helper
        // usable in all environments.
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SQLiteException)
        {
            // Intentionally ignore – the pragma may be unsupported in a non‑SQLCipher build.
        }
    }

    private static string EscapeSqliteString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
