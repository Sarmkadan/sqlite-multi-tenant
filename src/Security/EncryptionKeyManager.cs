#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Security
{
    /// <summary>
    /// Manages per-tenant AES-256 encryption keys with rotation and versioning support.
    /// Keys are stored as JSON files with restrictive permissions and cached in memory
    /// for fast retrieval. Supports master password derivation via PBKDF2 (SHA-256, 10k iterations).
    /// </summary>
    public sealed class EncryptionKeyManager {
        private readonly ILogger<EncryptionKeyManager> _logger;
        private readonly string _keyStorePath;
        private readonly ConcurrentDictionary<string, EncryptionKey> _keyCache;

        public EncryptionKeyManager(ILogger<EncryptionKeyManager> logger, string keyStorePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyStorePath = keyStorePath ?? throw new ArgumentNullException(nameof(keyStorePath));
            _keyCache = new ConcurrentDictionary<string, EncryptionKey>();

            Directory.CreateDirectory(_keyStorePath);
        }

        /// <summary>
        /// Generates a new 256-bit encryption key for a tenant. If a master password is provided,
        /// additional entropy is derived via PBKDF2 and XORed into the key material.
        /// </summary>
        /// <param name="tenantId">The tenant to generate the key for.</param>
        /// <param name="masterPassword">Optional master password for additional key derivation.</param>
        /// <returns>The newly created <see cref="EncryptionKey"/> with version 1.</returns>
        public async Task<EncryptionKey> GenerateKeyAsync(string tenantId, string masterPassword = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var key = new EncryptionKey
                {
                    KeyId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    KeyMaterial = GenerateRandomBytes(32), // 256-bit key
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Version = 1
                };

                // If master password provided, derive additional entropy
                if (!string.IsNullOrEmpty(masterPassword))
                {
                    using (var kdf = new Rfc2898DeriveBytes(masterPassword, 16, 10000, HashAlgorithmName.SHA256))
                    {
                        var derived = kdf.GetBytes(32);
                        for (int i = 0; i < key.KeyMaterial.Length; i++)
                        {
                            key.KeyMaterial[i] ^= derived[i];
                        }
                    }
                }

                await SaveKeyAsync(key);
                _keyCache.AddOrUpdate(tenantId, key, (_, __) => key);

                _logger.LogInformation("Encryption key generated for tenant: {TenantId}", tenantId);
                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate encryption key for tenant: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the currently active encryption key for a tenant.
        /// Results are cached in memory after the first file-system read.
        /// </summary>
        /// <param name="tenantId">The tenant to look up.</param>
        /// <returns>The active <see cref="EncryptionKey"/>, or <c>null</c> if no active key exists.</returns>
        public async Task<EncryptionKey> GetActiveKeyAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            // Check cache first
            if (_keyCache.TryGetValue(tenantId, out var cachedKey))
            {
                return cachedKey;
            }

            try
            {
                var keyPath = Path.Combine(_keyStorePath, $"{tenantId}_key.json");

                if (!File.Exists(keyPath))
                {
                    _logger.LogWarning("No encryption key found for tenant: {TenantId}", tenantId);
                    return null;
                }

                var json = await File.ReadAllTextAsync(keyPath);
                var key = JsonSerializer.Deserialize<EncryptionKey>(json);

                if (key?.IsActive == true)
                {
                    _keyCache.AddOrUpdate(tenantId, key, (_, __) => key);
                    return key;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve encryption key for tenant: {TenantId}", tenantId);
                return null;
            }
        }

        /// <summary>
        /// Rotates the encryption key for a tenant. The previous key is deactivated and
        /// archived with a version suffix. The new key's version is incremented automatically.
        /// </summary>
        /// <param name="tenantId">The tenant whose key should be rotated.</param>
        /// <param name="masterPassword">Optional master password for key derivation.</param>
        /// <returns>The newly generated <see cref="EncryptionKey"/> with incremented version.</returns>
        public async Task<EncryptionKey> RotateKeyAsync(string tenantId, string masterPassword = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            try
            {
                var oldKey = await GetActiveKeyAsync(tenantId);
                if (oldKey is not null)
                {
                    oldKey.IsActive = false;
                    oldKey.DeactivatedAt = DateTime.UtcNow;
                    await SaveKeyAsync(oldKey);
                }

                var newKey = new EncryptionKey
                {
                    KeyId = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    KeyMaterial = GenerateRandomBytes(32),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Version = (oldKey?.Version ?? 0) + 1,
                    PreviousKeyId = oldKey?.KeyId
                };

                // Apply master password if provided
                if (!string.IsNullOrEmpty(masterPassword))
                {
                    using (var kdf = new Rfc2898DeriveBytes(masterPassword, 16, 10000, HashAlgorithmName.SHA256))
                    {
                        var derived = kdf.GetBytes(32);
                        for (int i = 0; i < newKey.KeyMaterial.Length; i++)
                        {
                            newKey.KeyMaterial[i] ^= derived[i];
                        }
                    }
                }

                await SaveKeyAsync(newKey);
                _keyCache.AddOrUpdate(tenantId, newKey, (_, __) => newKey);

                _logger.LogInformation("Encryption key rotated for tenant: {TenantId} (Version: {Version})",
                    tenantId, newKey.Version);

                return newKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate encryption key for tenant: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific historical key version, typically used during re-encryption
        /// when migrating data from an older key to the current active key.
        /// </summary>
        /// <param name="tenantId">The tenant to look up.</param>
        /// <param name="version">The key version number to retrieve.</param>
        /// <returns>The <see cref="EncryptionKey"/> for the specified version, or <c>null</c> if not found.</returns>
        public async Task<EncryptionKey> GetKeyVersionAsync(string tenantId, int version)
        {
            try
            {
                var keyPath = Path.Combine(_keyStorePath, $"{tenantId}_key_v{version}.json");

                if (!File.Exists(keyPath))
                {
                    _logger.LogWarning("Key version {Version} not found for tenant: {TenantId}", version, tenantId);
                    return null;
                }

                var json = await File.ReadAllTextAsync(keyPath);
                return JsonSerializer.Deserialize<EncryptionKey>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve key version {Version} for tenant: {TenantId}",
                    version, tenantId);
                return null;
            }
        }

        /// <summary>
        /// Removes all encryption keys (active and archived) for a tenant from both
        /// the filesystem and the in-memory cache. Typically called during tenant deprovisioning.
        /// </summary>
        /// <param name="tenantId">The tenant whose keys should be deleted.</param>
        /// <returns><c>true</c> if keys were deleted successfully; <c>false</c> on error.</returns>
        public async Task<bool> DeleteTenantKeysAsync(string tenantId)
        {
            try
            {
                var keyFiles = Directory.GetFiles(_keyStorePath, $"{tenantId}_key*.json");

                foreach (var file in keyFiles)
                {
                    File.Delete(file);
                }

                _keyCache.TryRemove(tenantId, out _);

                _logger.LogInformation("Encryption keys deleted for tenant: {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete encryption keys for tenant: {TenantId}", tenantId);
                return false;
            }
        }

        private async Task SaveKeyAsync(EncryptionKey key)
        {
            try
            {
                var filename = key.IsActive
                    ? $"{key.TenantId}_key.json"
                    : $"{key.TenantId}_key_v{key.Version}.json";

                var path = Path.Combine(_keyStorePath, filename);
                var json = JsonSerializer.Serialize(key, new JsonSerializerOptions { WriteIndented = true });

                // Set restrictive permissions (owner read/write only)
                await File.WriteAllTextAsync(path, json);

                var fileInfo = new FileInfo(path);
                if (!IsWindowsPlatform())
                {
                    // On Unix-like systems, set permissions to 600 (rw-------)
                    var unixFileInfo = new System.IO.UnixFileSystemInfo(path);
                    unixFileInfo.FileAccessPermissions = System.IO.FileAccessPermissions.UserRead
                        | System.IO.FileAccessPermissions.UserWrite;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save encryption key");
                throw;
            }
        }

        private byte[] GenerateRandomBytes(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return bytes;
            }
        }

        private bool IsWindowsPlatform() =>
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
    }

    /// <summary>
    /// Represents an encryption key with versioning metadata. Active keys are stored
    /// as {tenantId}_key.json; deactivated keys are archived as {tenantId}_key_v{version}.json.
    /// </summary>
    public sealed class EncryptionKey {
        /// <summary>Unique identifier for this key instance.</summary>
        public string KeyId { get; set; }
        /// <summary>The tenant this key belongs to.</summary>
        public string TenantId { get; set; }
        /// <summary>Raw 256-bit key bytes used for AES encryption.</summary>
        public byte[] KeyMaterial { get; set; }
        /// <summary>UTC timestamp when this key was generated.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>UTC timestamp when this key was deactivated, or <c>null</c> if still active.</summary>
        public DateTime? DeactivatedAt { get; set; }
        /// <summary>Whether this key is the current active key for the tenant.</summary>
        public bool IsActive { get; set; }
        /// <summary>Monotonically increasing version number, incremented on each rotation.</summary>
        public int Version { get; set; }
        /// <summary>KeyId of the previous key in the rotation chain, or <c>null</c> for version 1.</summary>
        public string PreviousKeyId { get; set; }
    }
}
