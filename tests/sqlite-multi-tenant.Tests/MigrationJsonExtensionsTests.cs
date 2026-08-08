#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Models;
using System.Text.Json;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Tests for the MigrationJsonExtensions class.
/// </summary>
public sealed class MigrationJsonExtensionsTests
{
    /// <summary>
    /// Creates a new Migration instance with default values for testing.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <returns>A new Migration instance.</returns>
    private static Migration CreateMigration(string migrationId = "mgr-001") =>
        new()
        {
            MigrationId = migrationId,
            DatabaseId = "db-001",
            Version = "1.0.0",
            Name = "InitialMigration",
            Description = "Initial database setup",
            UpScript = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY);",
            DownScript = "DROP TABLE TestTable;",
            Status = MigrationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExecutedAt = null,
            CompletedAt = null,
            RolledBackAt = null,
            ExecutedBy = null,
            ErrorMessage = null,
            ExecutionTimeMs = 0,
            ExecutionOrder = 1,
            IsRollbackable = true
        };

    /// <summary>
    /// Tests that ToJson throws ArgumentNullException when value is null.
    /// </summary>
    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenValueIsNull()
    {
        // Arrange
        Migration? value = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => value!.ToJson());
    }

    /// <summary>
    /// Tests that ToJson serializes a migration to JSON correctly.
    /// </summary>
    [Fact]
    public void ToJson_SerializesMigrationToJsonCorrectly()
    {
        // Arrange
        var migration = CreateMigration();

        // Act
        var json = migration.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();

        // Parse the JSON back to verify properties
        var parsed = JsonSerializer.Deserialize<Migration>(json);
        parsed.Should().NotBeNull();
        parsed!.MigrationId.Should().Be(migration.MigrationId);
        parsed.DatabaseId.Should().Be(migration.DatabaseId);
        parsed.Version.Should().Be(migration.Version);
        parsed.Name.Should().Be(migration.Name);
        parsed.Description.Should().Be(migration.Description);
        parsed.UpScript.Should().Be(migration.UpScript);
        parsed.DownScript.Should().Be(migration.DownScript);
        parsed.Status.Should().Be(migration.Status);
        parsed.CreatedAt.Should().BeCloseTo(migration.CreatedAt, TimeSpan.FromSeconds(1));
        parsed.ExecutedAt.Should().BeNull();
        parsed.CompletedAt.Should().BeNull();
        parsed.RolledBackAt.Should().BeNull();
        parsed.ExecutedBy.Should().BeNull();
        parsed.ErrorMessage.Should().BeNull();
        parsed.ExecutionTimeMs.Should().Be(0);
        parsed.ExecutionOrder.Should().Be(1);
        parsed.IsRollbackable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ToJson serializes a migration to indented JSON correctly when indented is true.
    /// </summary>
    [Fact]
    public void ToJson_SerializesMigrationToIndentedJsonCorrectly_WhenIndentedIsTrue()
    {
        // Arrange
        var migration = CreateMigration();

        // Act
        var json = migration.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();

        // Parse the JSON back to verify properties (ignoring formatting)
        var parsed = JsonSerializer.Deserialize<Migration>(json);
        parsed.Should().NotBeNull();
        parsed!.MigrationId.Should().Be(migration.MigrationId);
        parsed.DatabaseId.Should().Be(migration.DatabaseId);
        parsed.Version.Should().Be(migration.Version);
        parsed.Name.Should().Be(migration.Name);
        parsed.Description.Should().Be(migration.Description);
        parsed.UpScript.Should().Be(migration.UpScript);
        parsed.DownScript.Should().Be(migration.DownScript);
        parsed.Status.Should().Be(migration.Status);
        parsed.CreatedAt.Should().BeCloseTo(migration.CreatedAt, TimeSpan.FromSeconds(1));
        parsed.ExecutedAt.Should().BeNull();
        parsed.CompletedAt.Should().BeNull();
        parsed.RolledBackAt.Should().BeNull();
        parsed.ExecutedBy.Should().BeNull();
        parsed.ErrorMessage.Should().BeNull();
        parsed.ExecutionTimeMs.Should().Be(0);
        parsed.ExecutionOrder.Should().Be(1);
        parsed.IsRollbackable.Should().BeTrue();

        // Verify it's actually indented (contains newlines)
        json.Should().Contain("\n");
        json.Should().Contain("  "); // at least two spaces for indentation
    }

    /// <summary>
    /// Tests that FromJson returns null for null or whitespace input.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public void FromJson_ReturnsNull_ForNullOrWhitespaceInput(string? json)
    {
        // Act
        var result = MigrationJsonExtensions.FromJson(json!);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that FromJson deserializes valid JSON to a Migration instance correctly.
    /// </summary>
    [Fact]
    public void FromJson_DeserializesValidJsonToMigrationCorrectly()
    {
        // Arrange
        var migration = CreateMigration();
        var json = migration.ToJson();

        // Act
        var result = MigrationJsonExtensions.FromJson(json);

        // Assert
        result.Should().NotBeNull();
        result!.MigrationId.Should().Be(migration.MigrationId);
        result.DatabaseId.Should().Be(migration.DatabaseId);
        result.Version.Should().Be(migration.Version);
        result.Name.Should().Be(migration.Name);
        result.Description.Should().Be(migration.Description);
        result.UpScript.Should().Be(migration.UpScript);
        result.DownScript.Should().Be(migration.DownScript);
        result.Status.Should().Be(migration.Status);
        result.CreatedAt.Should().BeCloseTo(migration.CreatedAt, TimeSpan.FromSeconds(1));
        result.ExecutedAt.Should().BeNull();
        result.CompletedAt.Should().BeNull();
        result.RolledBackAt.Should().BeNull();
        result.ExecutedBy.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.ExecutionTimeMs.Should().Be(0);
        result.ExecutionOrder.Should().Be(1);
        result.IsRollbackable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that FromJson throws JsonException for invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_ThrowsJsonException_ForInvalidJson()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() => MigrationJsonExtensions.FromJson(invalidJson));
    }

    /// <summary>
    /// Tests that TryFromJson returns false and null output for null input.
    /// </summary>
    [Fact]
    public void TryFromJson_ReturnsFalseAndNullOutput_ForNullInput()
    {
        // Arrange
        string? json = null;

        // Act
        var result = MigrationJsonExtensions.TryFromJson(json!, out Migration? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryFromJson returns false and null output for whitespace input.
    /// </summary>
    [Fact]
    public void TryFromJson_ReturnsFalseAndNullOutput_ForWhitespaceInput()
    {
        // Arrange
        var json = "   ";

        // Act
        var result = MigrationJsonExtensions.TryFromJson(json, out Migration? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryFromJson returns true and correct output for valid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_ReturnsTrueAndCorrectOutput_ForValidJson()
    {
        // Arrange
        var migration = CreateMigration();
        var json = migration.ToJson();

        // Act
        var result = MigrationJsonExtensions.TryFromJson(json, out Migration? value);

        // Assert
        result.Should().BeTrue();
        value.Should().NotBeNull();
        value!.MigrationId.Should().Be(migration.MigrationId);
        value.DatabaseId.Should().Be(migration.DatabaseId);
        value.Version.Should().Be(migration.Version);
        value.Name.Should().Be(migration.Name);
        value.Description.Should().Be(migration.Description);
        value.UpScript.Should().Be(migration.UpScript);
        value.DownScript.Should().Be(migration.DownScript);
        value.Status.Should().Be(migration.Status);
        value.CreatedAt.Should().BeCloseTo(migration.CreatedAt, TimeSpan.FromSeconds(1));
        value.ExecutedAt.Should().BeNull();
        value.CompletedAt.Should().BeNull();
        value.RolledBackAt.Should().BeNull();
        value.ExecutedBy.Should().BeNull();
        value.ErrorMessage.Should().BeNull();
        value.ExecutionTimeMs.Should().Be(0);
        value.ExecutionOrder.Should().Be(1);
        value.IsRollbackable.Should().BeTrue();
    }

    /// <summary>
    /// Tests that TryFromJson returns false and null output for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_ReturnsFalseAndNullOutput_ForInvalidJson()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = MigrationJsonExtensions.TryFromJson(invalidJson, out Migration? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }
}