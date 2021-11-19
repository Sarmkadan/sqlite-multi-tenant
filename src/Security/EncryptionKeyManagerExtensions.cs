namespace SqliteMultiTenant.Security
{
    public static class EncryptionKeyManagerExtensions
    {
        /// <summary>
        /// Generates a new encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The generated encryption key.</returns>
        public static async Task<EncryptionKey> GenerateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(tenantId);

            return await manager.GenerateKeyAsync(tenantId);
        }

        /// <summary>
        /// Rotates the encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The rotated encryption key.</returns>
        public static async Task<EncryptionKey> RotateKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(tenantId);

            return await manager.RotateKeyAsync(tenantId);
        }

        /// <summary>
        /// Gets the active encryption key for the specified tenant.
        /// </summary>
        /// <param name="manager">The encryption key manager.</param>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <returns>The active encryption key.</returns>
        public static async Task<EncryptionKey> GetActiveKeyForTenantAsync(this EncryptionKeyManager manager, string tenantId)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(tenantId);

            return await manager.GetActiveKeyAsync(tenantId);
        }
    }
}
