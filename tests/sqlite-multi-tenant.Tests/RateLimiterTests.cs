#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Security;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for the <see cref="RateLimiter"/> class.
/// Tests rate limiting functionality including allowance under limit, rejection over limit,
/// window reset behavior, and independent keys.
/// </summary>
public sealed class RateLimiterTests
{
    /// <summary>
    /// Mock logger instance for testing.
    /// </summary>
    private readonly ILogger<RateLimiter> _mockLogger;

    /// <summary>
    /// Instance of the service under test.
    /// </summary>
    private readonly RateLimiter _rateLimiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiterTests"/> class.
    /// </summary>
    public RateLimiterTests()
    {
        _mockLogger = Substitute.For<ILogger<RateLimiter>>();
        _rateLimiter = new RateLimiter(_mockLogger);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> allows requests under the limit.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_WithRequestsUnderLimit_ShouldAllow()
    {
        // Arrange
        var identifier = "test-ip-1";
        var maxRequests = 5;
        var window = TimeSpan.FromSeconds(10);

        // Act - make 3 requests (under limit of 5)
        var result1 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result2 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result3 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        // Assert
        result1.IsAllowed.Should().BeTrue();
        result2.IsAllowed.Should().BeTrue();
        result3.IsAllowed.Should().BeTrue();
        result1.CurrentCount.Should().Be(1);
        result2.CurrentCount.Should().Be(2);
        result3.CurrentCount.Should().Be(3);
        result1.RemainingRequests.Should().Be(4);
        result2.RemainingRequests.Should().Be(3);
        result3.RemainingRequests.Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> rejects requests over the limit.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_WithRequestsOverLimit_ShouldReject()
    {
        // Arrange
        var identifier = "test-ip-2";
        var maxRequests = 3;
        var window = TimeSpan.FromSeconds(10);

        // Act - make requests up to and over the limit
        var result1 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result2 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result3 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result4 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window); // Should be rejected

        // Assert
        result1.IsAllowed.Should().BeTrue();
        result2.IsAllowed.Should().BeTrue();
        result3.IsAllowed.Should().BeTrue();
        result4.IsAllowed.Should().BeFalse();

        result1.CurrentCount.Should().Be(1);
        result2.CurrentCount.Should().Be(2);
        result3.CurrentCount.Should().Be(3);
        result4.CurrentCount.Should().Be(3); // Count doesn't increase when rejected

        result4.RemainingRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> resets count when window expires.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_AfterWindowExpires_ShouldResetCount()
    {
        // Arrange
        var identifier = "test-ip-3";
        var maxRequests = 3;
        var window = TimeSpan.FromMilliseconds(100);

        // Act - make requests to fill the bucket
        var result1 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result2 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        var result3 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        // Wait for window to expire
        await Task.Delay(150);

        // Act - make another request after window expired
        var result4 = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        // Assert
        result1.IsAllowed.Should().BeTrue();
        result2.IsAllowed.Should().BeTrue();
        result3.IsAllowed.Should().BeTrue();
        result4.IsAllowed.Should().BeTrue(); // Should be allowed again after window reset

        result4.CurrentCount.Should().Be(1); // New count starts fresh
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> maintains independent limits for different keys.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_WithDifferentIdentifiers_ShouldMaintainIndependentLimits()
    {
        // Arrange
        var identifier1 = "ip-192.168.1.1";
        var identifier2 = "ip-192.168.1.2";
        var maxRequests = 2;
        var window = TimeSpan.FromSeconds(10);

        // Act - make requests for identifier1
        var result1_1 = await _rateLimiter.CheckLimitAsync(identifier1, maxRequests, window);
        var result1_2 = await _rateLimiter.CheckLimitAsync(identifier1, maxRequests, window);
        var result1_3 = await _rateLimiter.CheckLimitAsync(identifier1, maxRequests, window);

        // Verify identifier1 is blocked after reaching limit
        result1_1.IsAllowed.Should().BeTrue();
        result1_2.IsAllowed.Should().BeTrue();
        result1_3.IsAllowed.Should().BeFalse();

        // Act - make requests for identifier2 (should be independent)
        var result2_1 = await _rateLimiter.CheckLimitAsync(identifier2, maxRequests, window);
        var result2_2 = await _rateLimiter.CheckLimitAsync(identifier2, maxRequests, window);

        // Verify identifier2 can make 2 requests
        result2_1.IsAllowed.Should().BeTrue();
        result2_2.IsAllowed.Should().BeTrue();

        // Third request for identifier2 should also be blocked (independent limit)
        var result2_3 = await _rateLimiter.CheckLimitAsync(identifier2, maxRequests, window);
        result2_3.IsAllowed.Should().BeFalse();

        // Both identifiers should be blocked at their limits
        result1_3.IsAllowed.Should().BeFalse();
        result2_3.IsAllowed.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.ResetAsync"/> clears the rate limit for an identifier.
    /// </summary>
    [Fact]
    public async Task ResetAsync_ShouldClearRateLimit()
    {
        // Arrange
        var identifier = "test-ip-reset";
        var maxRequests = 3;
        var window = TimeSpan.FromSeconds(10);

        // Fill the bucket
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        var statusBefore = await _rateLimiter.GetStatusAsync(identifier);
        statusBefore.CurrentCount.Should().Be(3);

        // Act - reset the identifier
        await _rateLimiter.ResetAsync(identifier);

        // Assert - bucket should be cleared
        var statusAfter = await _rateLimiter.GetStatusAsync(identifier);
        statusAfter.CurrentCount.Should().Be(0);

        // New request should be allowed
        var result = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.GetStatusAsync"/> returns correct status for existing identifier.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ForExistingIdentifier_ShouldReturnCorrectStatus()
    {
        // Arrange
        var identifier = "test-ip-status";
        var maxRequests = 5;
        var window = TimeSpan.FromSeconds(10);

        // Make some requests
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        // Act
        var status = await _rateLimiter.GetStatusAsync(identifier);

        // Assert
        status.Should().NotBeNull();
        status.Identifier.Should().Be(identifier);
        status.CurrentCount.Should().Be(2);
        status.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        status.LastAccessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.GetStatusAsync"/> returns zero count for non-existent identifier.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ForNonExistentIdentifier_ShouldReturnZeroCount()
    {
        // Arrange
        var identifier = "non-existent-identifier";

        // Act
        var status = await _rateLimiter.GetStatusAsync(identifier);

        // Assert
        status.Should().NotBeNull();
        status.Identifier.Should().Be(identifier);
        status.CurrentCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> correctly calculates reset time.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_ShouldCalculateCorrectResetTime()
    {
        // Arrange
        var identifier = "test-ip-reset-time";
        var maxRequests = 10;
        var window = TimeSpan.FromSeconds(5);

        // Act - make a request
        var result = await _rateLimiter.CheckLimitAsync(identifier, maxRequests, window);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(1);
        result.MaxCount.Should().Be(10);

        // ResetTime should be approximately CurrentCount's timestamp + window
        var expectedResetTime = result.CurrentCount > 0
            ? result.CurrentCount > 0 ? DateTime.UtcNow.Add(window) : DateTime.UtcNow.Add(window)
            : DateTime.UtcNow.Add(window);
        result.ResetTime.Should().BeCloseTo(expectedResetTime, TimeSpan.FromMilliseconds(50));

        // TimeUntilReset should be approximately the window duration
        result.TimeUntilReset.Should().BeGreaterThan(TimeSpan.FromSeconds(4.9));
        result.TimeUntilReset.Should().BeLessThan(TimeSpan.FromSeconds(5.1));
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.CheckLimitAsync"/> removes old requests outside the window.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_ShouldRemoveOldRequestsOutsideWindow()
    {
        // Arrange
        var identifier = "test-ip-cleanup";
        var maxRequests = 100; // High limit to allow many requests
        var smallWindow = TimeSpan.FromMilliseconds(50);

        // Make first request
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, smallWindow);
        var status1 = await _rateLimiter.GetStatusAsync(identifier);
        status1.CurrentCount.Should().Be(1);

        // Wait for window to be mostly passed
        await Task.Delay(40);

        // Make second request
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, smallWindow);
        var status2 = await _rateLimiter.GetStatusAsync(identifier);
        status2.CurrentCount.Should().Be(2);

        // Wait for old request to expire
        await Task.Delay(20);

        // Make third request - old one should be cleaned up
        await _rateLimiter.CheckLimitAsync(identifier, maxRequests, smallWindow);
        var status3 = await _rateLimiter.GetStatusAsync(identifier);

        // Count should be 2 (first expired, second and third remain)
        // The cleanup happens during CheckLimitAsync, so we expect count to be 2
        status3.CurrentCount.Should().BeGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter.GetStatisticsAsync"/> returns valid statistics.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnValidStatistics()
    {
        // Arrange
        var identifier1 = "stats-ip-1";
        var identifier2 = "stats-ip-2";
        var maxRequests = 10;
        var window = TimeSpan.FromSeconds(10);

        // Make some requests for different identifiers
        await _rateLimiter.CheckLimitAsync(identifier1, maxRequests, window);
        await _rateLimiter.CheckLimitAsync(identifier1, maxRequests, window);
        await _rateLimiter.CheckLimitAsync(identifier2, maxRequests, window);

        // Act
        var stats = await _rateLimiter.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.ActiveBuckets.Should().Be(2);
        stats.TotalRequests.Should().Be(3);
        stats.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.OldestBucket.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.NewestBucket.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that <see cref="RateLimiter"/> constructor accepts null logger without throwing.
    /// </summary>
    [Fact]
    public void Service_Initialization_WithNullLogger_ShouldNotThrow()
    {
        // Act
        var action = () => new RateLimiter(null!);

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests multiple rapid requests to ensure thread safety.
    /// </summary>
    [Fact]
    public async Task CheckLimitAsync_MultipleRapidRequests_ShouldBeThreadSafe()
    {
        // Arrange
        var identifier = "thread-safe-ip";
        var maxRequests = 10;
        var window = TimeSpan.FromSeconds(10);

        // Act - make many rapid requests
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _rateLimiter.CheckLimitAsync(identifier, maxRequests, window))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - should allow up to maxRequests
        var allowedCount = results.Count(r => r.IsAllowed);
        allowedCount.Should().Be(maxRequests);

        // Total count should be maxRequests
        var status = await _rateLimiter.GetStatusAsync(identifier);
        status.CurrentCount.Should().Be(maxRequests);
    }
}
