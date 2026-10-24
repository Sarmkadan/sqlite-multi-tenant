#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SqliteMultiTenant.Events;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains regression tests verifying that <see cref="EventBusImpl"/> invokes handlers in the tenant scope of the event, not the ambient publisher's tenant.
/// </summary>
public sealed class EventBusImplTenantScopeTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";

    /// <summary>
    /// Tests that a handler still observes the publishing tenant after the publisher has switched to another tenant.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenPublisherSwitchesTenantMidFlight_HandlerStillSeesPublishingTenant()
    {
        // Arrange
        var tenantContext = new TenantContextHelper(NullLogger<TenantContextHelper>.Instance);
        using var eventBus = new EventBusImpl(Substitute.For<ILogger<EventBusImpl>>(), 1000, tenantContext);
        var gate = new TaskCompletionSource();
        string? observedTenant = null;

        using var subscription = eventBus.Subscribe<OrderPlacedEvent>(async _ =>
        {
            await gate.Task;
            observedTenant = tenantContext.GetCurrentTenantId();
        });

        using var publisherScope = tenantContext.CreateScope(TenantA);
        var orderEvent = new OrderPlacedEvent { TenantId = TenantA };

        // Act
        var publishTask = eventBus.PublishAsync(orderEvent);
        using (tenantContext.CreateScope(TenantB))
        {
            gate.SetResult();
            await publishTask;
        }

        // Assert
        observedTenant.Should().Be(TenantA);
    }

    /// <summary>
    /// Tests that a handler observes the event's tenant even when the publisher's ambient tenant differs.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenAmbientTenantDiffersFromEvent_HandlerSeesEventTenant()
    {
        // Arrange
        var tenantContext = new TenantContextHelper(NullLogger<TenantContextHelper>.Instance);
        using var eventBus = new EventBusImpl(Substitute.For<ILogger<EventBusImpl>>(), 1000, tenantContext);
        string? observedTenant = null;

        using var subscription = eventBus.Subscribe<OrderPlacedEvent>(_ =>
        {
            observedTenant = tenantContext.GetCurrentTenantId();
            return Task.CompletedTask;
        });

        using var publisherScope = tenantContext.CreateScope(TenantB);
        var orderEvent = new OrderPlacedEvent { TenantId = TenantA };

        // Act
        await eventBus.PublishAsync(orderEvent);

        // Assert
        observedTenant.Should().Be(TenantA);
        tenantContext.GetCurrentTenantId().Should().Be(TenantB);
    }

    /// <summary>
    /// Tests that a handler keeps the publisher's ambient tenant when the event carries no tenant identifier.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenEventHasNoTenant_HandlerSeesAmbientTenant()
    {
        // Arrange
        var tenantContext = new TenantContextHelper(NullLogger<TenantContextHelper>.Instance);
        using var eventBus = new EventBusImpl(Substitute.For<ILogger<EventBusImpl>>(), 1000, tenantContext);
        string? observedTenant = null;

        using var subscription = eventBus.Subscribe<OrderPlacedEvent>(_ =>
        {
            observedTenant = tenantContext.GetCurrentTenantId();
            return Task.CompletedTask;
        });

        using var publisherScope = tenantContext.CreateScope(TenantB);
        var orderEvent = new OrderPlacedEvent();

        // Act
        await eventBus.PublishAsync(orderEvent);

        // Assert
        observedTenant.Should().Be(TenantB);
    }

    /// <summary>
    /// Test event used to exercise tenant-scoped handler invocation.
    /// </summary>
    private sealed class OrderPlacedEvent : DomainEvent
    {
        public OrderPlacedEvent() : base(nameof(OrderPlacedEvent))
        {
        }
    }
}
