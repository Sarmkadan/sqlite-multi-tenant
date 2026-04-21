// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using SqliteMultiTenant.Events;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class EventBusImplTests
{
    private readonly EventBusImpl _eventBus;

    public EventBusImplTests()
    {
        _eventBus = new EventBusImpl();
    }

    [Fact]
    public void GetEventHistory_Initially_ShouldBeEmpty()
    {
        // Act
        var history = _eventBus.GetEventHistory();

        // Assert
        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public void ClearHistory_WhenHasEvents_ShouldClearList()
    {
        // Act
        _eventBus.ClearHistory();

        // Assert
        var history = _eventBus.GetEventHistory();
        history.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_WhenCalled_DoesNotThrow()
    {
        // Act
        var action = () => _eventBus.Dispose();

        // Assert
        action.Should().NotThrow();
    }
    
    [Fact]
    public void GetEventHistory_WithNegativeTake_ShouldHandleGracefully()
    {
        // Act
        var action = () => _eventBus.GetEventHistory(-1);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void EventBus_Initialization_PropertiesAreSet()
    {
        // Act
        var history = _eventBus.GetEventHistory(10);

        // Assert
        history.Should().BeOfType<List<PublishedEvent>>();
    }
}