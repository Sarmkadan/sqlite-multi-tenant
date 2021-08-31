# IEncryptionService

The `IEncryptionService` interface defines a contract for cryptographic operations within the `sqlite-multi-tenant` project. It provides methods for encrypting and decrypting strings and byte arrays, hashing passwords, and verifying hashes. This service is implemented by the `EncryptionService` class to ensure secure data handling, particularly for tenant-specific sensitive information stored in SQLite databases.

## API

### `EncryptionService()`
**Purpose**: Initializes a new instance of the `EncryptionService` class.
**Parameters**: None.
**Return Value**: A new instance of `EncryptionService`.
**Throws**: None.

---

### `string Encrypt(string plainText)`
**Purpose**: Encrypts a plain text string into a secure ciphertext.
**Parameters**:
- `plainText` (`string`): The input string to encrypt. Must not be `null` or empty.
**Return Value**: The encrypted ciphertext as a base64-encoded string.
**Throws**:
- `ArgumentNullException`: Thrown if `plainText` is `null`.
- `ArgumentException`: Thrown if `plainText` is empty or whitespace.
- `CryptographicException`: Thrown if encryption fails due to underlying cryptographic errors.

---

### `string Decrypt(string cipherText)`
**Purpose**: Decrypts a ciphertext string back to its original plain text.
**Parameters**:
- `cipherText` (`string`): The base64-encoded ciphertext to decrypt. Must not be `null` or empty.
**Return Value**: The decrypted plain text.
**Throws**:
- `ArgumentNullException`: Thrown if `cipherText` is `null`.
- `ArgumentException`: Thrown if `cipherText` is empty or whitespace.
- `CryptographicException`: Thrown if decryption fails due to invalid ciphertext or cryptographic errors.

---

### `byte[] EncryptBytes(byte[] plainBytes)`
**Purpose**: Encrypts a byte array into a secure cipher byte array.
**Parameters**:
- `plainBytes` (`byte[]`): The input byte array to encrypt. Must not be `null` or empty.
**Return Value**: The encrypted cipher byte array.
**Throws**:
- `ArgumentNullException`: Thrown if `plainBytes` is `null`.
- `ArgumentException`: Thrown if `plainBytes` is empty.
- `CryptographicException`: Thrown if encryption fails due to underlying cryptographic errors.

---

### `byte[] DecryptBytes(byte[] cipherBytes)`
**Purpose**: Decrypts a cipher byte array back to its original plain byte array.
**Parameters**:
- `cipherBytes` (`byte[]`): The cipher byte array to decrypt. Must not be `null` or empty.
**Return Value**: The decrypted plain byte array.
**Throws**:
- `ArgumentNullException`: Thrown if `cipherBytes` is `null`.
- `ArgumentException`: Thrown if `cipherBytes` is empty.
- `CryptographicException`: Thrown if decryption fails due to invalid cipher bytes or cryptographic errors.

---

### `bool VerifyHash(string input, string hash)`
**Purpose**: Verifies whether the provided input matches the given hash.
**Parameters**:
- `input` (`string`): The input string to verify. Must not be `null`.
- `hash` (`string`): The hash to compare against. Must not be `null` or empty.
**Return Value**: `true` if the input matches the hash; otherwise, `false`.
**Throws**:
- `ArgumentNullException`: Thrown if `input` or `hash` is `null`.
- `ArgumentException`: Thrown if `hash` is empty or whitespace.

---

### `string HashPassword(string password)`
**Purpose**: Generates a secure hash of the provided password.
**Parameters**:
- `password` (`string`): The password to hash. Must not be `null` or empty.
**Return Value**: The hashed password as a base64-encoded string.
**Throws**:
- `ArgumentNullException`: Thrown if `password` is `null`.
- `ArgumentException`: Thrown if `password` is empty or whitespace.
- `CryptographicException`: Thrown if hashing fails due to underlying cryptographic errors.

## Usage

### Example 1: Encrypting and Decrypting Sensitive Data
