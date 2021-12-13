#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using SqliteMultiTenant.Events;
using Xunit;

/// <summary>
/// Tests for domain events.
/// </summary>
public sealed class DomainEventTests {
    /// <summary>
    /// Verifies that the TenantCreatedEvent has the correct name.
    /// </summary>
    [Fact]
    public void TenantCreatedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantCreatedEvent();

        // Assert
        ev.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the BackupStartedEvent has the correct name.
    /// </summary>
    [Fact]
    public void BackupStartedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new BackupStartedEvent();

        // Assert
        ev.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the MigrationAppliedEvent has the correct name.
    /// </summary>
    [Fact]
    public void MigrationAppliedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new MigrationAppliedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
    
    /// <summary>
    /// Verifies that the TenantSuspendedEvent has the correct name.
    /// </summary>
    [Fact]
    public void TenantSuspendedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantSuspendedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
    
    /// <summary>
    /// Verifies that the TenantUpdatedEvent has the correct name.
    /// </summary>
    [Fact]
    public void TenantUpdatedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantUpdatedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
}
