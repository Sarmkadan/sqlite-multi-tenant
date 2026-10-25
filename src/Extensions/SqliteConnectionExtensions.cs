using System;
using System.Collections.Generic;
using System.Data.SQLite;

/// <summary>
/// Provides helper methods for <see cref="SQLiteConnection"/> to inspect schema metadata and to read or write the <c>user_version</c>, page count and freelist count pragmas.
/// </summary>
public static class SqliteConnectionExtensions
{
    /// <summary>
    /// Determines whether a table with the specified name exists in the database.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <param name="tableName">The name of the table to look for. Leading and trailing white-space is trimmed before the lookup. The match is exact and case-sensitive.</param>
    /// <returns><see langword="true"/> if a table with the given name exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> or <paramref name="tableName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> is empty or consists only of white-space characters.</exception>
    /// <exception cref="SQLiteException">The metadata query failed.</exception>
    public static bool TableExists(SQLiteConnection conn, string tableName)
    {
        ArgumentNullException.ThrowIfNull(conn);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;", conn);
        cmd.Parameters.AddWithValue("@tableName", tableName.Trim());
        var result = cmd.ExecuteScalar();
        return Convert.ToInt64(result) > 0;
    }

    // Existing long-returning version (kept for compatibility)
    /// <summary>
    /// Gets the database <c>user_version</c> value as a 64-bit integer.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <returns>The current <c>user_version</c>, or 0 if the pragma returns no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The pragma query failed.</exception>
    public static long GetUserVersion(SQLiteConnection conn)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA user_version;", conn);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0L;
    }

    // New int-returning overload
    /// <summary>
    /// Gets the database <c>user_version</c> value as a 32-bit integer.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <returns>The current <c>user_version</c>, or 0 if the pragma returns no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="OverflowException">The stored <c>user_version</c> is outside the range of a 32-bit integer.</exception>
    /// <exception cref="SQLiteException">The pragma query failed.</exception>
    public static int GetUserVersionInt(SQLiteConnection conn)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA user_version;", conn);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    // Existing long-version setter (kept for compatibility)
    /// <summary>
    /// Sets the database <c>user_version</c> value using a 64-bit integer.
    /// </summary>
    /// <param name="conn">The open SQLite connection to update.</param>
    /// <param name="version">The version number to store in the database.</param>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The pragma statement failed.</exception>
    public static void SetUserVersion(SQLiteConnection conn, long version)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA user_version = @version;", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    // New int-version setter
    /// <summary>
    /// Sets the database <c>user_version</c> value using a 32-bit integer.
    /// </summary>
    /// <param name="conn">The open SQLite connection to update.</param>
    /// <param name="version">The version number to store in the database.</param>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The pragma statement failed.</exception>
    public static void SetUserVersion(SQLiteConnection conn, int version)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA user_version = @version;", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Gets the total number of pages in the database file, as reported by <c>PRAGMA page_count</c>.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <returns>The number of pages, or 0 if the pragma returns no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The pragma query failed.</exception>
    public static long GetPageCount(SQLiteConnection conn)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA page_count;", conn);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0L;
    }

    /// <summary>
    /// Gets the number of unused pages on the database freelist, as reported by <c>PRAGMA freelist_count</c>.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <returns>The number of free pages, or 0 if the pragma returns no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The pragma query failed.</exception>
    public static long GetFreelistCount(SQLiteConnection conn)
    {
        ArgumentNullException.ThrowIfNull(conn);

        using var cmd = new SQLiteCommand("PRAGMA freelist_count;", conn);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0L;
    }

    /// <summary>
    /// Gets the names of all tables in the database, sorted alphabetically. Internal tables such as <c>sqlite_sequence</c> are included.
    /// </summary>
    /// <param name="conn">The open SQLite connection to query.</param>
    /// <returns>A read-only list of table names. The list is empty if the database contains no tables.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conn"/> is <see langword="null"/>.</exception>
    /// <exception cref="SQLiteException">The metadata query failed.</exception>
    public static IReadOnlyList<string> GetTableNames(SQLiteConnection conn)
    {
        ArgumentNullException.ThrowIfNull(conn);

        var tables = new List<string>();
        using var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
            {
                tables.Add(reader.GetString(0));
            }
        }
        return tables;
    }
}
