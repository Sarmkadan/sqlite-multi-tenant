#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using Xunit;

/// <summary>
/// Tests for the Tenant class.
/// </summary>
public sealed class TenantTests
{
    /// <summary>
    /// Creates a new Tenant instance with the specified parameters.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="name">The tenant name.</param>
    /// <param name="description">The tenant description.</param>
    /// <param name="status">The tenant status.</param>
    /// <param name="contactEmail">The contact email.</param>
    /// <param name="databasePath">The database path.</param>
    /// <param name="maxConnections">The maximum connections.</param>
    /// <returns>A new Tenant instance.</returns>
    private static Tenant CreateTenant(
        string tenantId = "tenant-001",
        string name = "Test Tenant",
        string? description = null,
        TenantStatus status = TenantStatus.Active,
        string? contactEmail = null,
        string? databasePath = null,
        int maxConnections = 10)
    =>
        new Tenant
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Status = status,
            ContactEmail = contactEmail,
            DatabasePath = databasePath,
            MaxConnections = maxConnections
        };

    /// <summary>
    /// Tests that the Tenant constructor initializes properties with default values.
    /// </summary>
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Act
        var tenant = new Tenant();

        // Assert
        tenant.TenantId.Should().BeEmpty();
        tenant.Name.Should().BeEmpty();
        tenant.Description.Should().BeNull();
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenant.LastAccessedAt.Should().BeNull();
        tenant.ContactEmail.Should().BeNull();
        tenant.DatabasePath.Should().BeNull();
        tenant.IsDataIsolated.Should().BeTrue();
        tenant.MaxConnections.Should().Be(10);
        tenant.Metadata.Should().BeNull();
        tenant.Databases.Should().BeEmpty();
        tenant.Settings.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that TenantId property can be set and retrieved.
    /// </summary>
    [Fact]
    public void TenantId_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedId = "test-tenant-123";

        // Act
        tenant.TenantId = expectedId;

        // Assert
        tenant.TenantId.Should().Be(expectedId);
    }

    /// <summary>
    /// Tests that Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedName = "Production Tenant";

        // Act
        tenant.Name = expectedName;

        // Assert
        tenant.Name.Should().Be(expectedName);
    }

    /// <summary>
    /// Tests that Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedDescription = "Production environment for main application";

        // Act
        tenant.Description = expectedDescription;

        // Assert
        tenant.Description.Should().Be(expectedDescription);
    }

    /// <summary>
    /// Tests that Description can be set to null.
    /// </summary>
    [Fact]
    public void Description_SetToNull_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant(description: "Some description");

        // Act
        tenant.Description = null;

        // Assert
        tenant.Description.Should().BeNull();
    }

    /// <summary>
    /// Tests that Status property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Status_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedStatus = TenantStatus.Inactive;

        // Act
        tenant.Status = expectedStatus;

        // Assert
        tenant.Status.Should().Be(expectedStatus);
    }

    /// <summary>
    /// Tests that CreatedAt property can be set and retrieved.
    /// </summary>
    [Fact]
    public void CreatedAt_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        tenant.CreatedAt = expectedDate;

        // Assert
        tenant.CreatedAt.Should().Be(expectedDate);
    }

    /// <summary>
    /// Tests that UpdatedAt property can be set and retrieved.
    /// </summary>
    [Fact]
    public void UpdatedAt_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        tenant.UpdatedAt = expectedDate;

        // Assert
        tenant.UpdatedAt.Should().Be(expectedDate);
    }

    /// <summary>
    /// Tests that LastAccessedAt property can be set and retrieved.
    /// </summary>
    [Fact]
    public void LastAccessedAt_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedDate = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        tenant.LastAccessedAt = expectedDate;

        // Assert
        tenant.LastAccessedAt.Should().Be(expectedDate);
    }

    /// <summary>
    /// Tests that LastAccessedAt can be set to null.
    /// </summary>
    [Fact]
    public void LastAccessedAt_SetToNull_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.LastAccessedAt = DateTime.UtcNow;

        // Act
        tenant.LastAccessedAt = null;

        // Assert
        tenant.LastAccessedAt.Should().BeNull();
    }

    /// <summary>
    /// Tests that ContactEmail property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ContactEmail_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedEmail = "admin@tenant.com";

        // Act
        tenant.ContactEmail = expectedEmail;

        // Assert
        tenant.ContactEmail.Should().Be(expectedEmail);
    }

    /// <summary>
    /// Tests that ContactEmail can be set to null.
    /// </summary>
    [Fact]
    public void ContactEmail_SetToNull_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant(contactEmail: "test@example.com");

        // Act
        tenant.ContactEmail = null;

        // Assert
        tenant.ContactEmail.Should().BeNull();
    }

    /// <summary>
    /// Tests that DatabasePath property can be set and retrieved.
    /// </summary>
    [Fact]
    public void DatabasePath_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedPath = "/var/data/tenant1.db";

        // Act
        tenant.DatabasePath = expectedPath;

        // Assert
        tenant.DatabasePath.Should().Be(expectedPath);
    }

    /// <summary>
    /// Tests that DatabasePath can be set to null.
    /// </summary>
    [Fact]
    public void DatabasePath_SetToNull_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant(databasePath: "/var/data/tenant1.db");

        // Act
        tenant.DatabasePath = null;

        // Assert
        tenant.DatabasePath.Should().BeNull();
    }

    /// <summary>
    /// Tests that IsDataIsolated property can be set and retrieved.
    /// </summary>
    [Fact]
    public void IsDataIsolated_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();

        // Act
        tenant.IsDataIsolated = false;

        // Assert
        tenant.IsDataIsolated.Should().BeFalse();
    }

    /// <summary>
    /// Tests that MaxConnections property can be set and retrieved.
    /// </summary>
    [Fact]
    public void MaxConnections_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedMaxConnections = 25;

        // Act
        tenant.MaxConnections = expectedMaxConnections;

        // Assert
        tenant.MaxConnections.Should().Be(expectedMaxConnections);
    }

    /// <summary>
    /// Tests that Metadata property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Metadata_SetAndGet_ReturnsExpectedValue()
    {
        // Arrange
        var tenant = CreateTenant();
        var expectedMetadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        // Act
        tenant.Metadata = expectedMetadata;

        // Assert
        tenant.Metadata.Should().BeSameAs(expectedMetadata);
    }

    /// <summary>
    /// Tests that Metadata can be set to null.
    /// </summary>
    [Fact]
    public void Metadata_SetToNull_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.Metadata = new Dictionary<string, string> { { "key", "value" } };

        // Act
        tenant.Metadata = null;

        // Assert
        tenant.Metadata.Should().BeNull();
    }

    /// <summary>
    /// Tests that Validate method returns true for a valid tenant.
    /// </summary>
    [Fact]
    public void Validate_WithValidTenant_ReturnsTrueAndNoErrors()
    {
        // Arrange
        var tenant = CreateTenant(
            tenantId: "valid-tenant-id-1234567890",
            name: "Valid Tenant Name",
            status: TenantStatus.Active,
            maxConnections: 5);

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (empty TenantId).
    /// </summary>
    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant(tenantId: string.Empty);

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain("TenantId must be non-empty");
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (TenantId too long).
    /// </summary>
    [Fact]
    public void Validate_WithTenantIdTooLong_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant(tenantId: new string('x', TenantConstants.MaxTenantIdLength + 1));

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain($"TenantId must be non-empty and less than {TenantConstants.MaxTenantIdLength} characters");
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (empty Name).
    /// </summary>
    [Fact]
    public void Validate_WithEmptyName_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant(name: string.Empty);

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain("Name must be non-empty");
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (Name too long).
    /// </summary>
    [Fact]
    public void Validate_WithNameTooLong_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant(name: new string('x', TenantConstants.MaxTenantNameLength + 1));

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain($"Name must be non-empty and less than {TenantConstants.MaxTenantNameLength} characters");
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (MaxConnections <= 0).
    /// </summary>
    [Fact]
    public void Validate_WithMaxConnectionsZero_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant(maxConnections: 0);

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain("MaxConnections must be greater than zero");
    }

    /// <summary>
    /// Tests that Validate method returns false and appropriate errors for an invalid tenant (CreatedAt after UpdatedAt).
    /// </summary>
    [Fact]
    public void Validate_WithCreatedAfterUpdated_ReturnsFalseAndError()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.CreatedAt = DateTime.UtcNow.AddDays(1);
        tenant.UpdatedAt = DateTime.UtcNow;

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().ContainSingle()
            .Which.Should().Contain("CreatedAt cannot be after UpdatedAt");
    }

    /// <summary>
    /// Tests that Validate method returns false with multiple errors when tenant has multiple validation issues.
    /// </summary>
    [Fact]
    public void Validate_WithMultipleErrors_ReturnsFalseAndAllErrors()
    {
        // Arrange
        var tenant = CreateTenant(
            tenantId: string.Empty,
            name: string.Empty,
            maxConnections: 0);
        tenant.CreatedAt = DateTime.UtcNow.AddDays(1);
        tenant.UpdatedAt = DateTime.UtcNow;

        // Act
        var isValid = tenant.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that MarkAsAccessed method updates LastAccessedAt and UpdatedAt.
    /// </summary>
    [Fact]
    public void MarkAsAccessed_UpdatesTimestamps()
    {
        // Arrange
        var tenant = CreateTenant();
        var originalUpdatedAt = tenant.UpdatedAt;
        var originalLastAccessedAt = tenant.LastAccessedAt;

        // Wait a small amount to ensure timestamps will be different
        Thread.Sleep(10);

        // Act
        tenant.MarkAsAccessed();

        // Assert
        tenant.LastAccessedAt.Should().NotBeNull();
        tenant.LastAccessedAt.Should().BeAfter(originalLastAccessedAt ?? DateTime.MinValue);
        tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    /// <summary>
    /// Tests that MarkAsAccessed method sets LastAccessedAt when it was null.
    /// </summary>
    [Fact]
    public void MarkAsAccessed_SetsLastAccessedAtWhenNull()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.LastAccessedAt = null;

        var originalUpdatedAt = tenant.UpdatedAt;

        // Wait a small amount to ensure timestamps will be different
        Thread.Sleep(10);

        // Act
        tenant.MarkAsAccessed();

        // Assert
        tenant.LastAccessedAt.Should().NotBeNull();
        tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    /// <summary>
    /// Tests that Deactivate method sets Status to Inactive.
    /// </summary>
    [Fact]
    public void Deactivate_SetsStatusToInactive()
    {
        // Arrange
        var tenant = CreateTenant(status: TenantStatus.Active);

        // Act
        tenant.Deactivate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Inactive);
        tenant.UpdatedAt.Should().BeAfter(tenant.CreatedAt);
    }

    /// <summary>
    /// Tests that Deactivate method updates UpdatedAt.
    /// </summary>
    [Fact]
    public void Deactivate_UpdatesUpdatedAt()
    {
        // Arrange
        var tenant = CreateTenant(status: TenantStatus.Active);
        var originalUpdatedAt = tenant.UpdatedAt;

        // Wait a small amount to ensure timestamps will be different
        Thread.Sleep(10);

        // Act
        tenant.Deactivate();

        // Assert
        tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    /// <summary>
    /// Tests that Activate method sets Status to Active.
    /// </summary>
    [Fact]
    public void Activate_SetsStatusToActive()
    {
        // Arrange
        var tenant = CreateTenant(status: TenantStatus.Inactive);

        // Act
        tenant.Activate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.UpdatedAt.Should().BeAfter(tenant.CreatedAt);
    }

    /// <summary>
    /// Tests that Activate method updates UpdatedAt.
    /// </summary>
    [Fact]
    public void Activate_UpdatesUpdatedAt()
    {
        // Arrange
        var tenant = CreateTenant(status: TenantStatus.Inactive);
        var originalUpdatedAt = tenant.UpdatedAt;

        // Wait a small amount to ensure timestamps will be different
        Thread.Sleep(10);

        // Act
        tenant.Activate();

        // Assert
        tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    /// <summary>
    /// Tests that SetMetadata method adds new metadata entry.
    /// </summary>
    [Fact]
    public void SetMetadata_AddsNewEntry()
    {
        // Arrange
        var tenant = CreateTenant();

        // Act
        tenant.SetMetadata("environment", "production");

        // Assert
        tenant.Metadata.Should().NotBeNull();
        tenant.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("production");
        tenant.UpdatedAt.Should().BeAfter(tenant.CreatedAt);
    }

    /// <summary>
    /// Tests that SetMetadata method updates existing metadata entry.
    /// </summary>
    [Fact]
    public void SetMetadata_UpdatesExistingEntry()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.SetMetadata("environment", "development");
        var originalUpdatedAt = tenant.UpdatedAt;

        // Wait a small amount to ensure timestamps will be different
        Thread.Sleep(10);

        // Act
        tenant.SetMetadata("environment", "production");

        // Assert
        tenant.Metadata.Should().NotBeNull();
        tenant.Metadata.Should().ContainKey("environment").WhoseValue.Should().Be("production");
        tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    /// <summary>
    /// Tests that SetMetadata method initializes Metadata dictionary when null.
    /// </summary>
    [Fact]
    public void SetMetadata_InitializesNullMetadata()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.Metadata = null;

        // Act
        tenant.SetMetadata("key", "value");

        // Assert
        tenant.Metadata.Should().NotBeNull();
        tenant.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    /// <summary>
    /// Tests that GetMetadata method returns null when Metadata is null.
    /// </summary>
    [Fact]
    public void GetMetadata_WithNullMetadata_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.Metadata = null;

        // Act
        var result = tenant.GetMetadata("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetMetadata method returns null for nonexistent key.
    /// </summary>
    [Fact]
    public void GetMetadata_WithNonexistentKey_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.SetMetadata("existing", "value");

        // Act
        var result = tenant.GetMetadata("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetMetadata method returns correct value for existing key.
    /// </summary>
    [Fact]
    public void GetMetadata_WithExistingKey_ReturnsCorrectValue()
    {
        // Arrange
        var tenant = CreateTenant();
        tenant.SetMetadata("environment", "production");
        tenant.SetMetadata("region", "us-east-1");

        // Act
        var result = tenant.GetMetadata("environment");

        // Assert
        result.Should().Be("production");
    }
}