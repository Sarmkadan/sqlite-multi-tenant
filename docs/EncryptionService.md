# EncryptionService

The `EncryptionService` class provides AES-256 encryption and decryption services for sensitive data, along with password hashing and verification. It implements the `IEncryptionService` interface and uses configurable encryption options for key derivation and cryptographic operations.

## Algorithm and Parameters

The service uses the following cryptographic parameters defined in `EncryptionOptions`:

- **KeySize**: 256 bits (AES-256)
- **IvSize**: 128 bits (Initialization Vector for CBC mode)
- **SaltSize**: 128 bits (for password hashing)
- **Iterations**: 10,000 (PBKDF2 iterations)
- **DerivationSalt**: "SqliteMultiTenant" (fixed salt for key derivation from the encryption key)

These values are visible in the `EncryptionOptions` class and are used consistently throughout the encryption and hashing operations.

## Relation to IEncryptionService

`EncryptionService` implements the `IEncryptionService` interface defined in [`docs/IEncryptionService.md`](IEncryptionService.md). All public methods correspond directly to the interface members, ensuring a consistent contract for cryptographic operations across the application.

## Public Methods

### Constructor
```csharp
public EncryptionService(IConfiguration config, ILogger<EncryptionService> logger, EncryptionOptions? options = null)
```
Initializes a new instance of the `EncryptionService` class.
- **config**: Configuration provider containing the encryption key under "Encryption:Key"
- **logger**: Logger for diagnostic information
- **options**: Optional encryption options; defaults to a new `EncryptionOptions` instance if not provided

Throws `InvalidOperationException` if the encryption key is not configured or is less than 32 characters.

### String Encryption/Decryption
```csharp
public string Encrypt(string plainText)
```
Encrypts a plain text string using AES-256-CBC with PKCS7 padding.
- **plainText**: The input string to encrypt (must not be null or empty)
- **Returns**: Base64-encoded ciphertext string
- **Throws**: `ArgumentException` for null/empty input, `CryptographicException` for underlying cryptographic errors

```csharp
public string Decrypt(string cipherText)
```
Decrypts an AES-256-CBC encrypted string.
- **cipherText**: Base64-encoded ciphertext to decrypt (must not be null or empty)
- **Returns**: Decrypted plain text string
- **Throws**: `ArgumentException` for null/empty input, `CryptographicException` for invalid ciphertext or cryptographic errors

### Byte Array Encryption/Decryption
```csharp
public byte[] EncryptBytes(byte[] data)
```
Encrypts a raw byte array.
- **data**: Input byte array to encrypt (can be null, returns null)
- **Returns**: Encrypted byte array with IV prepended
- **Throws**: `CryptographicException` for cryptographic errors

```csharp
public byte[] DecryptBytes(byte[] data)
```
Decrypts an encrypted byte array.
- **data**: Encrypted byte array with IV prepended (must be at least IvSize/8 bytes)
- **Returns**: Decrypted plain byte array
- **Throws**: `InvalidOperationException` for invalid data length, `CryptographicException` for cryptographic errors

### Password Hashing
```csharp
public string HashPassword(string password)
```
Creates a secure hash of a password using PBKDF2 with a random salt.
- **password**: The password to hash (must not be null or empty)
- **Returns**: Base64-encoded string containing salt followed by derived key
- **Throws**: `ArgumentException` for null/empty input, `CryptographicException` for cryptographic errors

```csharp
public bool VerifyHash(string plainText, string hash)
```
Verifies that a plain text string matches a previously computed hash.
- **plainText**: The input string to verify
- **hash**: The base64-encoded hash to compare against (salt + derived key)
- **Returns**: `true` if the hash matches, `false` otherwise
- **Throws**: Logs errors but returns false for cryptographic exceptions

## Usage Example

### Encrypting and Decrypting a Connection String

```csharp
using SqliteMultiTenant.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Setup (typically done via dependency injection)
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string> {
        {"Encryption:Key", "my-very-secure-encryption-key-32-chars-long!!"}
    })
    .Build();

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<EncryptionService>();

var encryptionService = new EncryptionService(configuration, logger);

// Encrypt a sensitive connection string
string connectionString = "Data Source=tenant1.db;Cache=Shared";
string encrypted = encryptionService.Encrypt(connectionString);
Console.WriteLine($"Encrypted: {encrypted}");

// Decrypt when needed
string decrypted = encryptionService.Decrypt(encrypted);
Console.WriteLine($"Decrypted: {decrypted}");

// Secure password storage
string password = "MySecurePassword123!";
string hashed = encryptionService.HashPassword(password);
Console.WriteLine($"Hashed password: { hashed }");

// Verify password later
bool isValid = encryptionService.VerifyHash(password, hashed);
Console.WriteLine($"Password valid: {isValid}"); // True

// Wrong password
bool isWrong = encryptionService.VerifyHash("wrong", hashed);
Console.WriteLine($"Wrong password valid: {isWrong}"); // False
```

### Encrypting Raw Byte Data

```csharp
// Encrypt byte array
byte[] sensitiveData = Encoding.UTF8.GetBytes("secret information");
byte[] encryptedBytes = encryptionService.EncryptBytes(sensitiveData);

// Store or transmit encryptedBytes...

// Decrypt when needed
byte[] decryptedBytes = encryptionService.DecryptBytes(encryptedBytes);
string original = Encoding.UTF8.GetString(decryptedBytes);
// original == "secret information"
```