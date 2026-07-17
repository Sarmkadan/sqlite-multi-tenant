namespace SqliteMultiTenant.Security
{
    /// <summary>
    /// Provides extension methods for <see cref="EncryptionKeyManager"/> that enhance tenant-specific key operations
    /// with additional convenience methods and fluent-style APIs.
    /// </summary>
    public static class EncryptionKeyManagerExtensions
    {
        /// <summary>
        /// Generates a new encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The generated encryption key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="tenantId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> GenerateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.GenerateKeyAsync(tenantId);
        }

        /// <summary>
        /// Generates a new encryption key for the specified tenant with optional master password.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="masterPassword">Optional master password for additional key derivation.</param>
        /// <returns>The generated encryption key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> GenerateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId, string masterPassword)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.GenerateKeyAsync(tenantId, masterPassword);
        }

        /// <summary>
        /// Rotates the encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The rotated encryption key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="tenantId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> RotateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.RotateKeyAsync(tenantId);
        }

        /// <summary>
        /// Rotates the encryption key for the specified tenant with optional master password.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="masterPassword">Optional master password for key derivation.</param>
        /// <returns>The rotated encryption key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> RotateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId, string masterPassword)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.RotateKeyAsync(tenantId, masterPassword);
        }

        /// <summary>
        /// Gets the active encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The active encryption key, or <c>null</c> if no active key exists.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="tenantId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> GetActiveKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.GetActiveKeyAsync(tenantId);
        }

        /// <summary>
        /// Gets the active encryption key for the specified tenant, throwing if not found.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The active encryption key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="tenantId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no active key exists for the tenant.</exception>
        public static async Task<EncryptionKey> GetRequiredActiveKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            var key = await manager.GetActiveKeyAsync(tenantId);
            return key ?? throw new InvalidOperationException($"No active encryption key found for tenant '{tenantId}'.");
        }

        /// <summary>
        /// Gets a specific historical key version for the tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="version">The key version number to retrieve.</param>
        /// <returns>The encryption key for the specified version, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<EncryptionKey> GetKeyVersionForTenantAsync(this EncryptionKeyManager manager, string tenantId, int version)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.GetKeyVersionAsync(tenantId, version);
        }

        /// <summary>
        /// Deletes all encryption keys for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns><c>true</c> if keys were deleted successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<bool> DeleteTenantKeysAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            return await manager.DeleteTenantKeysAsync(tenantId);
        }

        /// <summary>
        /// Determines whether an active encryption key exists for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns><c>true</c> if an active key exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="tenantId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is empty or whitespace.</exception>
        public static async Task<bool> HasActiveKeyAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            var key = await manager.GetActiveKeyAsync(tenantId);
            return key is not null;
        }
    }
}