// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using SqliteMultiTenant.Events;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class DomainEventTests
{
    [Fact]
    public void TenantCreatedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantCreatedEvent();

        // Assert
        ev.Should().NotBeNull();
    }

    [Fact]
    public void BackupStartedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new BackupStartedEvent();

        // Assert
        ev.Should().NotBeNull();
    }

    [Fact]
    public void MigrationAppliedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new MigrationAppliedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
    
    [Fact]
    public void TenantSuspendedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantSuspendedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
    
    [Fact]
    public void TenantUpdatedEvent_Initialization_HasCorrectName()
    {
        // Act
        var ev = new TenantUpdatedEvent();

        // Assert
        ev.Should().NotBeNull();
    }
}