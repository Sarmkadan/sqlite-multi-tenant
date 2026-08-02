using System.Collections.Generic;
using System.Data.SQLite;

public static class SqliteConnectionExtensions
{
    public static bool TableExists(SQLiteConnection conn, string tableName)
    {
        if (conn == null) throw new System.ArgumentNullException(nameof(conn));
        if (string.IsNullOrWhiteSpace(tableName)) throw new System.ArgumentException("Value cannot be null or whitespace.", nameof(tableName));

        using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;", conn);
        cmd.Parameters.AddWithValue("@tableName", tableName.Trim());
        var result = cmd.ExecuteScalar();
        return Convert.ToInt64(result) > 0;
    }

    public static long GetUserVersion(SQLiteConnection conn)
    {
        if (conn == null) throw new System.ArgumentNullException(nameof(conn));

        using var cmd = new SQLiteCommand("PRAGMA user_version;", conn);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0L;
    }

    public static void SetUserVersion(SQLiteConnection conn, long version)
    {
        if (conn == null) throw new System.ArgumentNullException(nameof(conn));

        using var cmd = new SQLiteCommand("PRAGMA user_version = @version;", conn);
        cmd.Parameters.AddWithValue("@version", version);
        cmd.ExecuteNonQuery();
    }

    public static IReadOnlyList<string> GetTableNames(SQLiteConnection conn)
    {
        if (conn == null) throw new System.ArgumentNullException(nameof(conn));

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