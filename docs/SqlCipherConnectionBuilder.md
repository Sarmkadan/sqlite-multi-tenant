# SqlCipherConnectionBuilder

Provides helper methods for building SQLCipher-compatible connection strings and for applying encryption keys to newly opened SQLite connections.

## Public Methods

### BuildConnectionString

```csharp
public static string BuildConnectionString(string databasePath, string encryptionKey, int version = 4)
```

Builds a SQLCipher-compatible connection string that instructs SQLite to encrypt (or decrypt) the database file using `encryptionKey`.

**Parameters**
- `databasePath`: Absolute or relative path to the `.db` file.
- `encryptionKey`: The passphrase or raw hex key used by SQLCipher. Must not be null or empty.
- `version`: SQLCipher compatibility version (1–4). Defaults to `4`, which uses the most recent defaults. Adjust when interoperating with older SQLCipher databases.

**Returns**
A connection string with the `Password` parameter set.

**Exceptions**
- `ArgumentException`: If `databasePath` or `encryptionKey` is empty.

### ApplyEncryptionKeyAsync

```csharp
public static async Task ApplyEncryptionKeyAsync(
    SQLiteConnection connection,
    string encryptionKey,
    CancellationToken cancellationToken = default)
```

Applies a SQLCipher encryption key to an already-open connection by executing `PRAGMA key`. Call this immediately after opening the connection when the key cannot be embedded in the connection string.

**Parameters**
- `connection`: An open `SQLiteConnection`.
- `encryptionKey`: The passphrase or raw hex key.
- `cancellationToken`: Cancellation token.

**Exceptions**
- `ArgumentNullException`: If `connection` is null.
- `ArgumentException`: If `encryptionKey` is empty.

### RekeyAsync

```csharp
public static async Task RekeyAsync(
    SQLiteConnection connection,
    string newKey,
    CancellationToken cancellationToken = default)
```

Re-keys an open SQLCipher database to a new passphrase. The operation is applied in-place; the database file is re-encrypted atomically.

**Parameters**
- `connection`: An open and already unlocked `SQLiteConnection`.
- `newKey`: The new passphrase or raw hex key.
- `cancellationToken`: Cancellation token.

**Exceptions**
- `ArgumentNullException`: If `connection` is null.
- `ArgumentException`: If `newKey` is empty.

## Connection String Keys

The `BuildConnectionString` method emits a connection string containing the following keys:
- `Data Source`: Set to the provided `databasePath`.
- `Password`: Set to the provided `encryptionKey`.

## Usage Example

```csharp
using SqliteMultiTenant.Security;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

// Example usage of SqlCipherConnectionBuilder
var dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".db");
var key = "my-secret-key";

// Build a connection string
var connectionString = SqlCipherConnectionBuilder.BuildConnectionString(dbPath, key);

// Apply encryption key to a connection
await using var connection = new SQLiteConnection(connectionString);
await connection.OpenAsync();
await SqlCipherConnectionBuilder.ApplyEncryptionKeyAsync(connection, key);
await connection.CloseAsync();

// Rekey the database
await using var connection2 = new SQLiteConnection(connectionString);
await connection2.OpenAsync();
await SqlCipherConnectionBuilder.RekeyAsync(connection2, "new-key");
await connection2.CloseAsync();

// Clean up
File.Delete(dbPath);
```