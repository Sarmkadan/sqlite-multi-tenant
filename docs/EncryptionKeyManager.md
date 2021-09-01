# EncryptionKeyManager

`EncryptionKeyManager` is a sealed service class responsible for the lifecycle management of per-tenant encryption keys in a multi-tenant SQLite environment. It provides methods to generate new keys, retrieve the currently active key, perform key rotation, fetch specific historical key versions, and purge all cryptographic material associated with a tenant. The companion type `EncryptionKey` models the metadata and raw key material for a single key version.

## API

### EncryptionKeyManager

```
public sealed class EncryptionKeyManager
```

A stateless manager that orchestrates encryption key operations. Instantiation details are implementation-specific and not exposed through the public surface documented here.

#### Constructor

```
public EncryptionKeyManager(/* implementation-specific dependencies */)
```

Creates a new instance of the manager. The exact parameters are supplied by dependency injection or factory configuration and are not part of the public API contract.

#### GenerateKeyAsync

```csharp
public async Task<EncryptionKey> GenerateKeyAsync(string tenantId)
```

Generates a fresh, cryptographically random encryption key for the specified tenant. The new key becomes the active key for that tenant, and any previously active key is deactivated.

| Parameter | Type     | Description                                  |
|-----------|----------|----------------------------------------------|
| `tenantId`| `string` | The identifier of the tenant.                |

**Returns:** An `EncryptionKey` instance representing the newly generated key. The `IsActive` property will be `true`, `Version` will be incremented beyond any existing keys, and `PreviousKeyId` will reference the key that was active before this generation.

**Throws:** `ArgumentNullException` when `tenantId` is null. `TenantNotFoundException` when the tenant does not exist in the system.

#### GetActiveKeyAsync

```csharp
public async Task<EncryptionKey> GetActiveKeyAsync(string tenantId)
```

Retrieves the currently active encryption key for the given tenant. The active key is the one that should be used for all new encryption operations.

| Parameter | Type     | Description                                  |
|-----------|----------|----------------------------------------------|
| `tenantId`| `string` | The identifier of the tenant.                |

**Returns:** The active `EncryptionKey`, or `null` if no key has ever been generated for the tenant.

**Throws:** `ArgumentNullException` when `tenantId` is null.

#### RotateKeyAsync

```csharp
public async Task<EncryptionKey> RotateKeyAsync(string tenantId)
```

Performs a key rotation: generates a new key, marks it as active, and deactivates the previously active key. The old key material is preserved for decryption of historical data. Functionally equivalent to `GenerateKeyAsync` but carries the semantic intent of a scheduled or policy-driven rotation.

| Parameter | Type     | Description                                  |
|-----------|----------|----------------------------------------------|
| `tenantId`| `string` | The identifier of the tenant.                |

**Returns:** The newly generated `EncryptionKey` that is now the active key.

**Throws:** `ArgumentNullException` when `tenantId` is null. `TenantNotFoundException` when the tenant does not exist. `InvalidOperationException` when there is no existing active key to rotate (i.e., rotation requires a prior key to exist).

#### GetKeyVersionAsync

```csharp
public async Task<EncryptionKey> GetKeyVersionAsync(string tenantId, int version)
```

Retrieves a specific historical key version for a tenant. This enables decryption of data that was encrypted with a key that is no longer active.

| Parameter | Type     | Description                                  |
|-----------|----------|----------------------------------------------|
| `tenantId`| `string` | The identifier of the tenant.                |
| `version` | `int`    | The version number of the desired key.       |

**Returns:** The `EncryptionKey` matching the specified version, or `null` if no key with that version exists for the tenant.

**Throws:** `ArgumentNullException` when `tenantId` is null. `ArgumentOutOfRangeException` when `version` is less than 1.

#### DeleteTenantKeysAsync

```csharp
public async Task<bool> DeleteTenantKeysAsync(string tenantId)
```

Irreversibly removes all encryption keys (active and historical) for the specified tenant. After this operation, no cryptographic material remains for the tenant, and any data encrypted with those keys becomes permanently inaccessible.

| Parameter | Type     | Description                                  |
|-----------|----------|----------------------------------------------|
| `tenantId`| `string` | The identifier of the tenant.                |

**Returns:** `true` if keys were found and deleted; `false` if no keys existed for the tenant.

**Throws:** `ArgumentNullException` when `tenantId` is null.

---

### EncryptionKey

```
public sealed class EncryptionKey
```

An immutable data object representing a single versioned encryption key bound to a specific tenant.

| Member           | Type        | Description                                                                 |
|------------------|-------------|-----------------------------------------------------------------------------|
| `KeyId`          | `string`    | Unique identifier for this key version.                                     |
| `TenantId`       | `string`    | The tenant to which this key belongs.                                       |
| `KeyMaterial`    | `byte[]`    | The raw cryptographic key bytes.                                            |
| `CreatedAt`      | `DateTime`  | UTC timestamp when this key version was generated.                          |
| `DeactivatedAt`  | `DateTime?` | UTC timestamp when this key was deactivated, or `null` if still active.     |
| `IsActive`       | `bool`      | Indicates whether this is the currently active key for the tenant.          |
| `Version`        | `int`       | Monotonically increasing version number for this tenant's keys.             |
| `PreviousKeyId`  | `string`    | The `KeyId` of the key that was active immediately before this one, or `null` for the first key. |

## Usage

### Example 1: Initial Key Setup and Encryption

```csharp
// Assume manager is injected via DI
public async Task<byte[]> EncryptSensitiveData(
    EncryptionKeyManager keyManager,
    string tenantId,
    byte[] plaintext)
{
    // Retrieve the active key, or generate one if none exists
    EncryptionKey activeKey = await keyManager.GetActiveKeyAsync(tenantId);
    if (activeKey == null)
    {
        activeKey = await keyManager.GenerateKeyAsync(tenantId);
    }

    // Use activeKey.KeyMaterial with your AES implementation
    byte[] ciphertext = AesEncrypt(plaintext, activeKey.KeyMaterial);
    return ciphertext;
}
```

### Example 2: Scheduled Key Rotation with Historical Decryption

```csharp
public async Task<byte[]> RotateAndReEncrypt(
    EncryptionKeyManager keyManager,
    string tenantId,
    byte[] ciphertext,
    int keyVersionUsed)
{
    // Fetch the historical key that was used for encryption
    EncryptionKey oldKey = await keyManager.GetKeyVersionAsync(tenantId, keyVersionUsed);
    if (oldKey == null)
    {
        throw new InvalidOperationException("Historical key not found.");
    }

    // Decrypt with the old key
    byte[] plaintext = AesDecrypt(ciphertext, oldKey.KeyMaterial);

    // Rotate to a new active key
    EncryptionKey newKey = await keyManager.RotateKeyAsync(tenantId);

    // Re-encrypt with the new active key
    byte[] newCiphertext = AesEncrypt(plaintext, newKey.KeyMaterial);
    return newCiphertext;
}
```

## Notes

- **Key Material Exposure:** `KeyMaterial` is a raw byte array. Consumers must handle it with extreme care—zero memory after use, avoid logging, and never persist it outside the manager's own secure storage.
- **Rotation Semantics:** `RotateKeyAsync` requires an existing active key. Calling it on a tenant that has never had a key generated will throw `InvalidOperationException`. Use `GenerateKeyAsync` for initial provisioning.
- **Version Ordering:** Version numbers are tenant-scoped and strictly increasing. The first key generated for a tenant has version `1`, and each subsequent generation or rotation increments the version by `1`.
- **Deactivation Timing:** When a new key is generated or rotated in, the previously active key's `DeactivatedAt` is set to the current UTC time and `IsActive` becomes `false`. The new key's `PreviousKeyId` is set to the `KeyId` of that deactivated key.
- **DeleteTenantKeysAsync Irreversibility:** This operation performs a hard delete. There is no undo. Ensure that all data encrypted with the tenant's keys is either re-encrypted under a different tenant or explicitly no longer needed before calling this method.
- **Thread Safety:** All public methods are asynchronous and expected to be safe for concurrent use. The underlying implementation must handle concurrent calls for the same tenant without corrupting key state (e.g., two simultaneous rotations should not produce duplicate active keys). The exact synchronization mechanism is an implementation detail.
- **Null Returns:** `GetActiveKeyAsync` and `GetKeyVersionAsync` return `null` when no matching key exists. Callers must null-check before accessing properties like `KeyMaterial`.
- **Tenant Lifecycle:** The manager assumes tenants are managed externally. It does not create or delete tenants; it only manages keys for tenant identifiers it is given.
