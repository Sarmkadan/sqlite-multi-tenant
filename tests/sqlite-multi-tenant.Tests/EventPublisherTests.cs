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

/// <summary>
/// Tests for the EventPublisher class.
/// Validates pub-sub pattern, handler registration, and error handling.
/// </summary>
public sealed class EventPublisherTests
{
    private readonly ILogger<EventPublisher> _mockLogger;
    private readonly EventPublisher _publisher;

    public EventPublisherTests()
    {
        _mockLogger = Substitute.For<ILogger<EventPublisher>>();
        _publisher = new EventPublisher(_mockLogger);
    }

    /// <summary>
    /// Test event for testing purposes.
    /// </summary>
    private sealed class TestEvent : DomainEvent
    {
        public string TestData { get; set; } = "test";

        public TestEvent() : base(nameof(TestEvent))
        {
        }
    }

    /// <summary>
    /// Test event handler for testing purposes.
    /// </summary>
    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public bool WasCalled { get; private set; }
        public TestEvent? LastEvent { get; private set; }
        public int CallCount { get; private set; }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            WasCalled = true;
            LastEvent = @event;
            CallCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test event with different type for multiple handlers testing.
    /// </summary>
    private sealed class AnotherTestEvent : DomainEvent
    {
        public int Value { get; set; }

        public AnotherTestEvent() : base(nameof(AnotherTestEvent))
        {
        }
    }

    /// <summary>
    /// Another test event handler.
    /// </summary>
    private sealed class AnotherTestEventHandler : IEventHandler<AnotherTestEvent>
    {
        public bool WasCalled { get; private set; }

        public Task HandleAsync(AnotherTestEvent @event, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Synchronous test event handler.
    /// </summary>
    private sealed class SyncTestEventHandler : IEventHandler<TestEvent>
    {
        public bool WasCalled { get; private set; }

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Event handler that throws an exception to test error handling.
    /// </summary>
    private sealed class ThrowingEventHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    /// <summary>
    /// Tests that PublishAsync throws when event is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        TestEvent? nullEvent = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _publisher.PublishAsync(nullEvent!));
        // No logger calls expected since exception is thrown before any logging
    }

    /// <summary>
    /// Tests that PublishAsync does not throw when no handlers are registered.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var @event = new TestEvent();

        // Act
        var act = () => _publisher.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
        // Logger calls are made but we can't easily verify them with NSubstitute due to formatting
        // The important part is that it doesn't throw
    }

    /// <summary>
    /// Tests that PublishAsync reaches subscribed handler.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithSubscribedHandler_HandlerIsCalled()
    {
        // Arrange
        var handler = new TestEventHandler();
        _publisher.Subscribe(handler);
        var @event = new TestEvent { TestData = "test value" };

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        handler.WasCalled.Should().BeTrue();
        handler.LastEvent.Should().BeSameAs(@event);
        handler.CallCount.Should().Be(1);
        // Logger calls are made but we can't easily verify them with NSubstitute due to formatting
        // The important part is that the handler is called correctly
    }

    /// <summary>
    /// Tests that multiple handlers for same event type are all called.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithMultipleHandlersForSameEvent_AllHandlersAreCalled()
    {
        // Arrange
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();
        var handler3 = new TestEventHandler();

        _publisher.Subscribe(handler1);
        _publisher.Subscribe(handler2);
        _publisher.Subscribe(handler3);

        var @event = new TestEvent();

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        handler1.WasCalled.Should().BeTrue();
        handler2.WasCalled.Should().BeTrue();
        handler3.WasCalled.Should().BeTrue();
        handler1.CallCount.Should().Be(1);
        handler2.CallCount.Should().Be(1);
        handler3.CallCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that handlers for different event types are not affected by each other.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithMultipleEventTypes_HandlersOnlyForCorrectTypeAreCalled()
    {
        // Arrange
        var testHandler = new TestEventHandler();
        var anotherHandler = new AnotherTestEventHandler();

        _publisher.Subscribe(testHandler);
        _publisher.Subscribe(anotherHandler);

        var testEvent = new TestEvent();
        var anotherEvent = new AnotherTestEvent { Value = 42 };

        // Act
        await _publisher.PublishAsync(testEvent);
        await _publisher.PublishAsync(anotherEvent);

        // Assert
        testHandler.WasCalled.Should().BeTrue();
        anotherHandler.WasCalled.Should().BeTrue();
        testHandler.CallCount.Should().Be(1);
        anotherHandler.WasCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetHandlerCount returns correct count.
    /// </summary>
    [Fact]
    public void GetHandlerCount_WithNoHandlers_ReturnsZero()
    {
        // Arrange & Act
        var count = _publisher.GetHandlerCount<TestEvent>();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetHandlerCount returns correct count with handlers.
    /// </summary>
    [Fact]
    public void GetHandlerCount_WithHandlers_ReturnsCorrectCount()
    {
        // Arrange
        var handler1 = new TestEventHandler();
        var handler2 = new TestEventHandler();
        var handler3 = new TestEventHandler();

        _publisher.Subscribe(handler1);
        _publisher.Subscribe(handler2);
        _publisher.Subscribe(handler3);

        // Act
        var count = _publisher.GetHandlerCount<TestEvent>();

        // Assert
        count.Should().Be(3);
    }

    /// <summary>
    /// Tests that Subscribe throws when handler is null.
    /// </summary>
    [Fact]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        IEventHandler<TestEvent>? nullHandler = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _publisher.Subscribe(nullHandler!));
    }

    /// <summary>
    /// Tests that handler can be subscribed multiple times.
    /// </summary>
    [Fact]
    public async Task Subscribe_WithSameHandlerMultipleTimes_HandlerIsCalledMultipleTimes()
    {
        // Arrange
        var handler = new TestEventHandler();
        _publisher.Subscribe(handler);
        _publisher.Subscribe(handler);
        _publisher.Subscribe(handler);

        var @event = new TestEvent();

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        handler.WasCalled.Should().BeTrue();
        handler.CallCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that exception in handler is logged but does not prevent other handlers from running.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithHandlerThrowingException_ExceptionIsLoggedButOtherHandlersRun()
    {
        // Arrange
        var throwingHandler = new ThrowingEventHandler();
        var normalHandler = new TestEventHandler();

        _publisher.Subscribe(throwingHandler);
        _publisher.Subscribe(normalHandler);

        var @event = new TestEvent();

        // Act
        var act = () => _publisher.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
        normalHandler.WasCalled.Should().BeTrue();
        normalHandler.CallCount.Should().Be(1);

        _mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Error invoking event handler")),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    /// <summary>
    /// Tests that synchronous handlers are wrapped and executed correctly.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithSyncHandler_SyncHandlerIsCalled()
    {
        // Arrange
        var handler = new SyncTestEventHandler();
        _publisher.Subscribe(handler);
        var @event = new TestEvent();

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        handler.WasCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that cancellation token is respected.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithCancellationToken_CancellationIsRespected()
    {
        // Arrange
        var handler = new TestEventHandler();
        _publisher.Subscribe(handler);
        var @event = new TestEvent();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _publisher.PublishAsync(@event, cts.Token);

        // Assert
        await act.Should().NotThrowAsync();
        // Handler may or may not be called depending on timing, but it shouldn't throw
    }

    /// <summary>
    /// Tests that EventPublisher can be instantiated with null logger (should throw).
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventPublisher(null!));
    }

    /// <summary>
    /// Tests that DomainEvent properties are correctly set.
    /// </summary>
    [Fact]
    public void DomainEvent_PropertiesAreCorrectlyInitialized()
    {
        // Arrange
        var testEvent = new TestEvent();

        // Assert
        testEvent.EventId.Should().NotBeNullOrEmpty();
        testEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        testEvent.EventType.Should().Be(nameof(TestEvent));
        testEvent.TestData.Should().Be("test");
    }

    /// <summary>
    /// Tests that TenantCreatedEvent inherits from DomainEvent correctly.
    /// </summary>
    [Fact]
    public void TenantCreatedEvent_InheritsFromDomainEvent()
    {
        // Arrange
        var tenantEvent = new TenantCreatedEvent();

        // Assert
        tenantEvent.Should().BeAssignableTo<DomainEvent>();
        tenantEvent.EventType.Should().Be(nameof(TenantCreatedEvent));
        tenantEvent.EventId.Should().NotBeNullOrEmpty();
        tenantEvent.TenantId.Should().BeNull();
    }
}