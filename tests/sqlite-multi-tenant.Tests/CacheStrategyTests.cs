#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Caching;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Unit tests for <see cref="LruCacheStrategy"/> and <see cref="TimeBasedCacheStrategy"/>
    /// covering public policy decisions (eviction/expiry selection) with boundary values.
    /// </summary>
    public class CacheStrategyTests
    {
        private readonly ILogger<LruCacheStrategy> _lruLoggerMock;
        private readonly ILogger<TimeBasedCacheStrategy> _timeBasedLoggerMock;

        public CacheStrategyTests()
        {
            _lruLoggerMock = Substitute.For<ILogger<LruCacheStrategy>>();
            _timeBasedLoggerMock = Substitute.For<ILogger<TimeBasedCacheStrategy>>();
        }

        #region LruCacheStrategy Tests

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> evicts the least recently used item when cache is full.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_EvictsLeastRecentlyUsed_WhenCacheIsFull()
        {
            // Arrange
            const int maxSize = 2;
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy eviction test with max size {MaxSize}", maxSize);
            var cache = new LruCacheStrategy(_lruLoggerMock, maxSize);

            // Fill cache to capacity
            await cache.SetAsync("key1", "value1");
            await cache.SetAsync("key2", "value2");

            // Access key1 to make it recently used
            await cache.GetAsync<string>("key1");

            // Add third item - should evict key2 (least recently used)
            await cache.SetAsync("key3", "value3");

            // Assert
            var value1 = await cache.GetAsync<string>("key1");
            var value2 = await cache.GetAsync<string>("key2");
            var value3 = await cache.GetAsync<string>("key3");

            value1.Should().Be("value1"); // Should still exist (recently used)
            value2.Should().BeNull();     // Should be evicted (LRU)
            value3.Should().Be("value3"); // Should exist (just added)

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy eviction test; key1={Key1}, key2={Key2}, key3={Key3}", value1, value2, value3);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> respects boundary value of maxSize = 1.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_HandlesMaxSizeOfOne_Correctly()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy max size test with max size 1");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 1);

            // Act
            await cache.SetAsync("key1", "value1");
            await cache.SetAsync("key2", "value2"); // Should evict key1

            // Assert
            var value1 = await cache.GetAsync<string>("key1");
            var value2 = await cache.GetAsync<string>("key2");

            value1.Should().BeNull(); // Should be evicted
            value2.Should().Be("value2"); // Should exist

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy max size test; key1={Key1}, key2={Key2}", value1, value2);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> expires entries based on TTL.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_ExpiresEntriesBasedOnTTL()
        {
            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);
            var shortLivedValue = "short-lived";
            var ttl = TimeSpan.FromMilliseconds(10);
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy expiration test with TTL {TTL}", ttl);

            // Act
            await cache.SetAsync("shortKey", shortLivedValue, ttl);
            await Task.Delay(20); // Wait for expiration

            // Assert
            var result = await cache.GetAsync<string>("shortKey");
            result.Should().BeNull(); // Should be expired

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy expiration test; shortKey={ShortKey}", result);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> handles zero TTL (immediate expiration).
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_HandlesZeroTTL_ImmediateExpiration()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy zero TTL test");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);

            // Act
            await cache.SetAsync("zeroTTLKey", "value", TimeSpan.Zero);
            var result = await cache.GetAsync<string>("zeroTTLKey");

            // Assert
            result.Should().BeNull(); // Should be immediately expired

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy zero TTL test; result={Result}", result);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> handles null expiration (no expiration).
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_HandlesNullExpiration_NoExpiration()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy null expiration test");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);

            // Act
            await cache.SetAsync("noExpirationKey", "value", null);
            var result = await cache.GetAsync<string>("noExpirationKey");

            // Assert
            result.Should().Be("value"); // Should not expire

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy null expiration test; result={Result}", result);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> updates access count on get operations.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_UpdatesAccessCount_OnGetOperations()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy access count test");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);
            await cache.SetAsync("testKey", "testValue");
            await cache.GetAsync<string>("testKey"); // First access (SetAsync also counts as access)

            // Act
            await cache.GetAsync<string>("testKey"); // Second access
            await cache.GetAsync<string>("testKey"); // Third access

            // Assert
            var stats = cache.GetStatistics();
            stats.Should().ContainKey("testKey");
            stats["testKey"].AccessCount.Should().Be(4);

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy access count test; accessCount={AccessCount}", stats["testKey"].AccessCount);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> handles null key gracefully.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_HandlesNullKey_ReturnsDefault()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy null key test");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);

            // Act
            var result = await cache.GetAsync<string>(null!);

            // Assert
            result.Should().BeNull();

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy null key test; result={Result}", result);
        }

        /// <summary>
        /// Tests that <see cref="LruCacheStrategy"/> handles empty key gracefully.
        /// </summary>
        [Fact]
        public async Task LruCacheStrategy_HandlesEmptyKey_ReturnsDefault()
        {
            _lruLoggerMock.LogInformation("Starting LruCacheStrategy empty key test");

            // Arrange
            var cache = new LruCacheStrategy(_lruLoggerMock, 100);

            // Act
            var result = await cache.GetAsync<string>("");

            // Assert
            result.Should().BeNull();

            _lruLoggerMock.LogInformation("Completed LruCacheStrategy empty key test; result={Result}", result);
        }

        #endregion

        #region TimeBasedCacheStrategy Tests

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> uses default expiration when none provided.
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_UsesDefaultExpiration_WhenNotProvided()
        {
            _timeBasedLoggerMock.LogInformation("Starting TimeBasedCacheStrategy default expiration test");

            // Arrange
            var defaultExpiration = TimeSpan.FromMinutes(30);
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, defaultExpiration);

            // Act
            await cache.SetAsync("testKey", "testValue"); // No expiration specified
            await Task.Delay(TimeSpan.FromMilliseconds(10)); // Small delay

            // Assert
            var result = await cache.GetAsync<string>("testKey");
            result.Should().Be("testValue"); // Should still exist (not expired yet)

            _timeBasedLoggerMock.LogInformation("Completed TimeBasedCacheStrategy default expiration test; result={Result}", result);
        }

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> expires entries based on provided TTL.
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_ExpiresEntriesBasedOnProvidedTTL()
        {
            _timeBasedLoggerMock.LogInformation("Starting TimeBasedCacheStrategy provided TTL expiration test");

            // Arrange
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, TimeSpan.FromHours(1));
            var shortLivedValue = "expires-soon";

            // Act
            await cache.SetAsync("shortKey", shortLivedValue, TimeSpan.FromMilliseconds(10));
            await Task.Delay(20); // Wait for expiration

            // Assert
            var result = await cache.GetAsync<string>("shortKey");
            result.Should().BeNull(); // Should be expired

            _timeBasedLoggerMock.LogInformation("Completed TimeBasedCacheStrategy provided TTL expiration test; shortKey={ShortKey}", result);
        }

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> handles zero TTL (immediate expiration).
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_HandlesZeroTTL_ImmediateExpiration()
        {
            // Arrange
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, TimeSpan.FromHours(1));

            // Act
            await cache.SetAsync("zeroTTLKey", "value", TimeSpan.Zero);
            var result = await cache.GetAsync<string>("zeroTTLKey");

            // Assert
            result.Should().BeNull(); // Should be immediately expired
        }

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> handles null expiration (uses default).
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_HandlesNullExpiration_UsesDefault()
        {
            // Arrange
            var defaultExpiration = TimeSpan.FromMinutes(5);
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, defaultExpiration);

            // Act
            await cache.SetAsync("defaultExpirationKey", "value", null);
            await Task.Delay(TimeSpan.FromMilliseconds(10)); // Small delay

            // Assert
            var result = await cache.GetAsync<string>("defaultExpirationKey");
            result.Should().Be("value"); // Should still exist (using default expiration)
        }

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> handles null key gracefully.
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_HandlesNullKey_ReturnsDefault()
        {
            // Arrange
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, TimeSpan.FromHours(1));

            // Act
            var result = await cache.GetAsync<string>(null!);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that <see cref="TimeBasedCacheStrategy"/> handles empty key gracefully.
        /// </summary>
        [Fact]
        public async Task TimeBasedCacheStrategy_HandlesEmptyKey_ReturnsDefault()
        {
            // Arrange
            var cache = new TimeBasedCacheStrategy(_timeBasedLoggerMock, TimeSpan.FromHours(1));

            // Act
            var result = await cache.GetAsync<string>("");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}