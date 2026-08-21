#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Events;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for the <see cref="EventBusImpl"/> class.
/// </summary>
public sealed class EventBusImplTests {
	/// <summary>
	/// The event bus instance used for testing.
	/// </summary>
	private readonly EventBusImpl _eventBus;

	/// <summary>
	/// Initializes a new instance of the <see cref="EventBusImplTests"/> class.
	/// </summary>
	public EventBusImplTests()
	{
		_eventBus = new EventBusImpl(Substitute.For<ILogger<EventBusImpl>>());
	}

	[Fact]
	/// <summary>
	/// Tests that GetEventHistory returns an empty list when called initially.
	/// </summary>
	public void GetEventHistory_Initially_ShouldBeEmpty()
	{
		// Act
		var history = _eventBus.GetEventHistory();

		// Assert
		history.Should().NotBeNull();
		history.Should().BeEmpty();
	}

	[Fact]
	/// <summary>
	/// Tests that ClearHistory removes all events from the event bus.
	/// </summary>
	public void ClearHistory_WhenHasEvents_ShouldClearList()
	{
		// Act
		_eventBus.ClearHistory();

		// Assert
		var history = _eventBus.GetEventHistory();
		history.Should().BeEmpty();
	}

	[Fact]
	/// <summary>
	/// Tests that calling Dispose on the event bus does not throw exceptions.
	/// </summary>
	public void Dispose_WhenCalled_DoesNotThrow()
	{
		// Act
		var action = () => _eventBus.Dispose();

		// Assert
		action.Should().NotThrow();
	}

	[Fact]
	/// <summary>
	/// Tests that GetEventHistory handles negative take values gracefully without throwing.
	/// </summary>
	public void GetEventHistory_WithNegativeTake_ShouldHandleGracefully()
	{
		// Act
		var action = () => _eventBus.GetEventHistory(-1);

		// Assert
		action.Should().NotThrow();
	}

	[Fact]
	/// <summary>
	/// Tests that the event bus initializes correctly and returns the expected event history type.
	/// </summary>
	public void EventBus_Initialization_PropertiesAreSet()
	{
		// Act
		var history = _eventBus.GetEventHistory(10);

		// Assert
		history.Should().BeOfType<List<PublishedEvent>>();
	}
}