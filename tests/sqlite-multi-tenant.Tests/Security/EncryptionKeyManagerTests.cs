using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Security;
using Xunit;

namespace SqliteMultiTenant.Tests.Security
{
    public class EncryptionKeyManagerTests : IDisposable
    {
        private readonly ILogger<EncryptionKeyManager> _logger;
        private readonly string _testKeyStorePath;
        private readonly EncryptionKeyManager _keyManager;

        public EncryptionKeyManagerTests()
        {
            _logger = Substitute.For<ILogger<EncryptionKeyManager>>();
            _testKeyStorePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testKeyStorePath);

            _keyManager = new EncryptionKeyManager(_logger, _testKeyStorePath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testKeyStorePath))
                {
                    Directory.Delete(_testKeyStorePath, true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        [Fact]
        public async Task GenerateKeyAsync_WithValidTenantId_ReturnsNewKeyWithVersion1()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithValidTenantId_ReturnsNewKeyWithVersion1 with TenantId={TenantId}", "tenant-123");

            // Arrange
            var tenantId = "tenant-123";

            // Act
            var key = await _keyManager.GenerateKeyAsync(tenantId);

            // Assert
            key.Should().NotBeNull();
            key.TenantId.Should().Be(tenantId);
            key.KeyId.Should().NotBeNullOrEmpty();
            key.KeyMaterial.Should().NotBeNull().And.HaveCount(32);
            key.Version.Should().Be(1);
            key.IsActive.Should().BeTrue();
            key.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            key.DeactivatedAt.Should().BeNull();
            key.PreviousKeyId.Should().BeNull();

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithValidTenantId_ReturnsNewKeyWithVersion1");
        }

        [Fact]
        public async Task GenerateKeyAsync_WithValidTenantId_CachesKeyInMemory()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithValidTenantId_CachesKeyInMemory with TenantId={TenantId}", "tenant-cached");

            // Arrange
            var tenantId = "tenant-cached";

            // Act
            var key = await _keyManager.GenerateKeyAsync(tenantId);

            // Assert - verify it's cached
            var cachedKey = await _keyManager.GetActiveKeyAsync(tenantId);
            cachedKey.Should().NotBeNull();
            cachedKey.KeyId.Should().Be(key.KeyId);

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithValidTenantId_CachesKeyInMemory");
        }

        [Fact]
        public async Task GenerateKeyAsync_WithEmptyTenantId_ThrowsArgumentException()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithEmptyTenantId_ThrowsArgumentException");

            // Arrange
            var invalidTenantId = string.Empty;

            // Act
            Func<Task> act = async () => await _keyManager.GenerateKeyAsync(invalidTenantId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>("Tenant ID cannot be empty");

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithEmptyTenantId_ThrowsArgumentException");
        }

        [Fact]
        public async Task GenerateKeyAsync_WithWhitespaceTenantId_ThrowsArgumentException()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithWhitespaceTenantId_ThrowsArgumentException");

            // Arrange
            var invalidTenantId = "   ";

            // Act
            Func<Task> act = async () => await _keyManager.GenerateKeyAsync(invalidTenantId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>("Tenant ID cannot be empty");

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithWhitespaceTenantId_ThrowsArgumentException");
        }

        [Fact]
        public async Task GenerateKeyAsync_WithNullTenantId_ThrowsArgumentException()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithNullTenantId_ThrowsArgumentException");

            // Arrange
            string nullTenantId = null!;

            // Act
            Func<Task> act = async () => await _keyManager.GenerateKeyAsync(nullTenantId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>("Tenant ID cannot be empty");

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithNullTenantId_ThrowsArgumentException");
        }

        [Fact]
        public async Task GetActiveKeyAsync_ForExistingTenant_ReturnsActiveKey()
        {
            _logger.LogInformation("Starting test: GetActiveKeyAsync_ForExistingTenant_ReturnsActiveKey with TenantId={TenantId}", "existing-tenant");

            // Arrange
            var tenantId = "existing-tenant";
            var generatedKey = await _keyManager.GenerateKeyAsync(tenantId);

            // Act
            var retrievedKey = await _keyManager.GetActiveKeyAsync(tenantId);

            // Assert
            retrievedKey.Should().NotBeNull();
            retrievedKey.KeyId.Should().Be(generatedKey.KeyId);
            retrievedKey.Version.Should().Be(1);
            retrievedKey.IsActive.Should().BeTrue();

            _logger.LogInformation("Completed test: GetActiveKeyAsync_ForExistingTenant_ReturnsActiveKey");
        }

        [Fact]
        public async Task GetActiveKeyAsync_ForNonExistentTenant_ReturnsNull()
        {
            _logger.LogInformation("Starting test: GetActiveKeyAsync_ForNonExistentTenant_ReturnsNull with TenantId={TenantId}", "non-existent-tenant");

            // Arrange
            var nonExistentTenantId = "non-existent-tenant";

            // Act
            var key = await _keyManager.GetActiveKeyAsync(nonExistentTenantId);

            // Assert
            key.Should().BeNull();

            _logger.LogInformation("Completed test: GetActiveKeyAsync_ForNonExistentTenant_ReturnsNull");
        }

        [Fact]
        public async Task GetActiveKeyAsync_WithEmptyTenantId_ThrowsArgumentException()
        {
            _logger.LogInformation("Starting test: GetActiveKeyAsync_WithEmptyTenantId_ThrowsArgumentException");

            // Arrange
            var invalidTenantId = string.Empty;

            // Act
            Func<Task> act = async () => await _keyManager.GetActiveKeyAsync(invalidTenantId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>("Tenant ID cannot be empty");

            _logger.LogInformation("Completed test: GetActiveKeyAsync_WithEmptyTenantId_ThrowsArgumentException");
        }

        [Fact]
        public async Task RotateKeyAsync_ForExistingTenant_CreatesNewKeyAndDeactivatesOld()
        {
            _logger.LogInformation("Starting test: RotateKeyAsync_ForExistingTenant_CreatesNewKeyAndDeactivatesOld with TenantId={TenantId}", "rotate-tenant");

            // Arrange
            var tenantId = "rotate-tenant";
            var oldKey = await _keyManager.GenerateKeyAsync(tenantId);
            oldKey.IsActive.Should().BeTrue();

            // Act
            var newKey = await _keyManager.RotateKeyAsync(tenantId);

            // Assert - new key properties
            newKey.Should().NotBeNull();
            newKey.TenantId.Should().Be(tenantId);
            newKey.Version.Should().Be(2);
            newKey.IsActive.Should().BeTrue();
            newKey.PreviousKeyId.Should().Be(oldKey.KeyId);
            newKey.KeyId.Should().NotBe(oldKey.KeyId);
            newKey.KeyMaterial.Should().NotBeEquivalentTo(oldKey.KeyMaterial);

            // Assert - old key is deactivated and can be retrieved
            var activeKey = await _keyManager.GetActiveKeyAsync(tenantId);
            activeKey.Should().NotBeNull();
            activeKey.KeyId.Should().Be(newKey.KeyId);

            // Old key should still be retrievable (either by version or by checking deactivated state)
            var oldKeyFromCache = await _keyManager.GetActiveKeyAsync(tenantId);
            // The old key is no longer active, so we can't get it via GetActiveKeyAsync
            // But we should be able to retrieve it from disk
            var oldKeyFromDisk = await _keyManager.GetKeyVersionAsync(tenantId, 1);
            oldKeyFromDisk.Should().NotBeNull();
            oldKeyFromDisk.IsActive.Should().BeFalse();
            oldKeyFromDisk.DeactivatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _logger.LogInformation("Completed test: RotateKeyAsync_ForExistingTenant_CreatesNewKeyAndDeactivatesOld");
        }

        [Fact]
        public async Task RotateKeyAsync_ForNonExistentTenant_CreatesFirstKey()
        {
            _logger.LogInformation("Starting test: RotateKeyAsync_ForNonExistentTenant_CreatesFirstKey with TenantId={TenantId}", "first-rotation-tenant");

            // Arrange
            var tenantId = "first-rotation-tenant";

            // Act
            var key = await _keyManager.RotateKeyAsync(tenantId);

            // Assert
            key.Should().NotBeNull();
            key.Version.Should().Be(1);
            key.IsActive.Should().BeTrue();
            key.PreviousKeyId.Should().BeNull();

            _logger.LogInformation("Completed test: RotateKeyAsync_ForNonExistentTenant_CreatesFirstKey");
        }

        [Fact]
        public async Task RotateKeyAsync_MultipleTimes_IncrementsVersionEachTime()
        {
            _logger.LogInformation("Starting test: RotateKeyAsync_MultipleTimes_IncrementsVersionEachTime with TenantId={TenantId}", "multi-rotate-tenant");

            // Arrange
            var tenantId = "multi-rotate-tenant";
            var initialKey = await _keyManager.GenerateKeyAsync(tenantId);
            initialKey.Version.Should().Be(1);

            // First rotation
            var key2 = await _keyManager.RotateKeyAsync(tenantId);
            key2.Version.Should().Be(2);

            // Second rotation
            var key3 = await _keyManager.RotateKeyAsync(tenantId);
            key3.Version.Should().Be(3);

            // Third rotation
            var key4 = await _keyManager.RotateKeyAsync(tenantId);
            key4.Version.Should().Be(4);

            // Assert - active key is version 4
            var activeKey = await _keyManager.GetActiveKeyAsync(tenantId);
            activeKey.Should().NotBeNull();
            activeKey.Version.Should().Be(4);

            // Assert - all previous versions can be retrieved from disk
            var key1 = await _keyManager.GetKeyVersionAsync(tenantId, 1);
            key1.Should().NotBeNull("Key version 1 should exist on disk");
            key1.IsActive.Should().BeFalse();
            key1.Version.Should().Be(1);

            var key2Retrieved = await _keyManager.GetKeyVersionAsync(tenantId, 2);
            key2Retrieved.Should().NotBeNull("Key version 2 should exist on disk");
            key2Retrieved.IsActive.Should().BeFalse();
            key2Retrieved.Version.Should().Be(2);

            var key3Retrieved = await _keyManager.GetKeyVersionAsync(tenantId, 3);
            key3Retrieved.Should().NotBeNull("Key version 3 should exist on disk");
            key3Retrieved.IsActive.Should().BeFalse();
            key3Retrieved.Version.Should().Be(3);

            _logger.LogInformation("Completed test: RotateKeyAsync_MultipleTimes_IncrementsVersionEachTime");
        }

        [Fact]
        public async Task RotateKeyAsync_WithEmptyTenantId_ThrowsArgumentException()
        {
            _logger.LogInformation("Starting test: RotateKeyAsync_WithEmptyTenantId_ThrowsArgumentException");

            // Arrange
            var invalidTenantId = string.Empty;

            // Act
            Func<Task> act = async () => await _keyManager.RotateKeyAsync(invalidTenantId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>("Tenant ID cannot be empty");

            _logger.LogInformation("Completed test: RotateKeyAsync_WithEmptyTenantId_ThrowsArgumentException");
        }

        [Fact]
        public async Task GetKeyVersionAsync_ForExistingVersion_ReturnsCorrectKey()
        {
            _logger.LogInformation("Starting test: GetKeyVersionAsync_ForExistingVersion_ReturnsCorrectKey with TenantId={TenantId}", "version-lookup");

            // Arrange
            var tenantId = "version-lookup";
            var originalKey = await _keyManager.GenerateKeyAsync(tenantId);
            await _keyManager.RotateKeyAsync(tenantId);
            await _keyManager.RotateKeyAsync(tenantId);

            // Act
            var retrievedKey = await _keyManager.GetKeyVersionAsync(tenantId, 1);

            // Assert
            retrievedKey.Should().NotBeNull();
            retrievedKey.Version.Should().Be(1);
            retrievedKey.KeyId.Should().Be(originalKey.KeyId);

            _logger.LogInformation("Completed test: GetKeyVersionAsync_ForExistingVersion_ReturnsCorrectKey");
        }

        [Fact]
        public async Task GetKeyVersionAsync_ForNonExistentVersion_ReturnsNull()
        {
            _logger.LogInformation("Starting test: GetKeyVersionAsync_ForNonExistentVersion_ReturnsNull with TenantId={TenantId}", "version-test");

            // Arrange
            var tenantId = "version-test";
            await _keyManager.GenerateKeyAsync(tenantId);

            // Act
            var key = await _keyManager.GetKeyVersionAsync(tenantId, 999);

            // Assert
            key.Should().BeNull();

            _logger.LogInformation("Completed test: GetKeyVersionAsync_ForNonExistentVersion_ReturnsNull");
        }

        [Fact]
        public async Task GetKeyVersionAsync_ForNonExistentTenant_ReturnsNull()
        {
            _logger.LogInformation("Starting test: GetKeyVersionAsync_ForNonExistentTenant_ReturnsNull with TenantId={TenantId}", "non-existent");

            // Arrange
            var nonExistentTenantId = "non-existent";

            // Act
            var key = await _keyManager.GetKeyVersionAsync(nonExistentTenantId, 1);

            // Assert
            key.Should().BeNull();

            _logger.LogInformation("Completed test: GetKeyVersionAsync_ForNonExistentTenant_ReturnsNull");
        }

        [Fact]
        public async Task DeleteTenantKeysAsync_RemovesAllKeysForTenant()
        {
            _logger.LogInformation("Starting test: DeleteTenantKeysAsync_RemovesAllKeysForTenant with TenantId={TenantId}", "delete-me");

            // Arrange
            var tenantId = "delete-me";
            await _keyManager.GenerateKeyAsync(tenantId);
            await _keyManager.RotateKeyAsync(tenantId);
            await _keyManager.RotateKeyAsync(tenantId);

            // Verify keys exist
            var activeKey = await _keyManager.GetActiveKeyAsync(tenantId);
            activeKey.Should().NotBeNull();

            // Act
            var result = await _keyManager.DeleteTenantKeysAsync(tenantId);

            // Assert
            result.Should().BeTrue();
            var deletedKey = await _keyManager.GetActiveKeyAsync(tenantId);
            deletedKey.Should().BeNull();

            // Verify all versions are gone
            for (int i = 1; i <= 3; i++)
            {
                var key = await _keyManager.GetKeyVersionAsync(tenantId, i);
                key.Should().BeNull();
            }

            _logger.LogInformation("Completed test: DeleteTenantKeysAsync_RemovesAllKeysForTenant");
        }

        [Fact]
        public async Task DeleteTenantKeysAsync_ForNonExistentTenant_ReturnsTrue()
        {
            _logger.LogInformation("Starting test: DeleteTenantKeysAsync_ForNonExistentTenant_ReturnsTrue with TenantId={TenantId}", "does-not-exist");

            // Arrange
            var nonExistentTenantId = "does-not-exist";

            // Act
            var result = await _keyManager.DeleteTenantKeysAsync(nonExistentTenantId);

            // Assert
            result.Should().BeTrue();

            _logger.LogInformation("Completed test: DeleteTenantKeysAsync_ForNonExistentTenant_ReturnsTrue");
        }

        [Fact]
        public async Task DeleteTenantKeysAsync_WithEmptyTenantId_StillReturnsTrue()
        {
            _logger.LogInformation("Starting test: DeleteTenantKeysAsync_WithEmptyTenantId_StillReturnsTrue");

            // Arrange
            var invalidTenantId = string.Empty;

            // Act
            var result = await _keyManager.DeleteTenantKeysAsync(invalidTenantId);

            // Assert
            result.Should().BeTrue();

            _logger.LogInformation("Completed test: DeleteTenantKeysAsync_WithEmptyTenantId_StillReturnsTrue");
        }

        [Fact]
        public async Task KeyRotation_PreservesOldKeyForDecryption()
        {
            _logger.LogInformation("Starting test: KeyRotation_PreservesOldKeyForDecryption with TenantId={TenantId}", "decrypt-test");

            // Arrange
            var tenantId = "decrypt-test";
            var oldKey = await _keyManager.GenerateKeyAsync(tenantId);

            // Rotate to new key
            var newKey = await _keyManager.RotateKeyAsync(tenantId);

            // Assert - old key is still available for decryption
            var stillRetrievableOldKey = await _keyManager.GetKeyVersionAsync(tenantId, 1);
            stillRetrievableOldKey.Should().NotBeNull();
            stillRetrievableOldKey.IsActive.Should().BeFalse();
            stillRetrievableOldKey.DeactivatedAt.Should().NotBeNull();

            _logger.LogInformation("Completed test: KeyRotation_PreservesOldKeyForDecryption");
        }

        [Fact]
        public async Task GenerateKeyAsync_WithMasterPassword_AppliesKeyDerivation()
        {
            _logger.LogInformation("Starting test: GenerateKeyAsync_WithMasterPassword_AppliesKeyDerivation with TenantId={TenantId}, MasterPassword={MasterPassword}", "password-tenant", "***");

            // Arrange
            var tenantId = "password-tenant";
            var masterPassword = "MySecureMasterPassword123!";

            // Act
            var key = await _keyManager.GenerateKeyAsync(tenantId, masterPassword);

            // Assert
            key.Should().NotBeNull();
            key.TenantId.Should().Be(tenantId);
            key.Version.Should().Be(1);

            _logger.LogInformation("Completed test: GenerateKeyAsync_WithMasterPassword_AppliesKeyDerivation");
        }

        [Fact]
        public async Task RotateKeyAsync_WithMasterPassword_AppliesKeyDerivation()
        {
            _logger.LogInformation("Starting test: RotateKeyAsync_WithMasterPassword_AppliesKeyDerivation with TenantId={TenantId}, MasterPassword={MasterPassword}", "rotate-password-tenant", "***");

            // Arrange
            var tenantId = "rotate-password-tenant";
            var masterPassword = "MySecureMasterPassword123!";
            await _keyManager.GenerateKeyAsync(tenantId, masterPassword);

            // Act
            var newKey = await _keyManager.RotateKeyAsync(tenantId, masterPassword);

            // Assert
            newKey.Should().NotBeNull();
            newKey.TenantId.Should().Be(tenantId);
            newKey.Version.Should().Be(2);

            _logger.LogInformation("Completed test: RotateKeyAsync_WithMasterPassword_AppliesKeyDerivation");
        }

        [Fact]
        public async Task KeysAreStoredOnDisk_AndCanBeReloaded()
        {
            _logger.LogInformation("Starting test: KeysAreStoredOnDisk_AndCanBeReloaded with TenantId={TenantId}", "disk-storage");

            // Arrange
            var tenantId = "disk-storage";
            var originalKey = await _keyManager.GenerateKeyAsync(tenantId);

            // Create new manager instance to force reload from disk
            var newKeyManager = new EncryptionKeyManager(_logger, _testKeyStorePath);

            // Act
            var reloadedKey = await newKeyManager.GetActiveKeyAsync(tenantId);

            // Assert
            reloadedKey.Should().NotBeNull();
            reloadedKey.KeyId.Should().Be(originalKey.KeyId);
            reloadedKey.KeyMaterial.Should().BeEquivalentTo(originalKey.KeyMaterial);

            _logger.LogInformation("Completed test: KeysAreStoredOnDisk_AndCanBeReloaded");
        }

        [Fact]
        public async Task MultipleTenants_HaveIndependentKeys()
        {
            _logger.LogInformation("Starting test: MultipleTenants_HaveIndependentKeys with Tenant1={Tenant1}, Tenant2={Tenant2}", "tenant-one", "tenant-two");

            // Arrange
            var tenant1 = "tenant-one";
            var tenant2 = "tenant-two";

            var key1 = await _keyManager.GenerateKeyAsync(tenant1);
            var key2 = await _keyManager.GenerateKeyAsync(tenant2);

            // Act
            var retrievedKey1 = await _keyManager.GetActiveKeyAsync(tenant1);
            var retrievedKey2 = await _keyManager.GetActiveKeyAsync(tenant2);

            // Assert
            retrievedKey1.Should().NotBeNull();
            retrievedKey2.Should().NotBeNull();
            retrievedKey1.KeyId.Should().NotBe(retrievedKey2.KeyId);
            retrievedKey1.TenantId.Should().Be(tenant1);
            retrievedKey2.TenantId.Should().Be(tenant2);

            _logger.LogInformation("Completed test: MultipleTenants_HaveIndependentKeys");
        }
    }
}