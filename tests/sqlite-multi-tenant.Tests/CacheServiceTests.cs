#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Caching;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class CacheServiceTests {
        private readonly IMemoryCache _mockMemoryCache;
        private readonly ILogger<CacheService> _mockLogger;
        private readonly CacheService _sut;

        public CacheServiceTests()
        {
            _mockMemoryCache = Substitute.For<IMemoryCache>();
            _mockLogger = Substitute.For<ILogger<CacheService>>();
            _sut = new CacheService(_mockMemoryCache, _mockLogger);
        }

        [Fact]
        public void CacheService_Constructor_ThrowsArgumentNullException_WhenMemoryCacheIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new CacheService(null, _mockLogger))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("cache");
        }

        [Fact]
        public void CacheService_Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            this.Invoking(_ => new CacheService(_mockMemoryCache, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Get_ExistingKey_ReturnsValue()
        {
            // Arrange
            var key = "testKey";
            var expectedValue = "testValue";
            _mockMemoryCache.TryGetValue(key, out Arg.Any<object>())
                .Returns(x => { x[1] = expectedValue; return true; });

            // Act
            var result = _sut.Get<string>(key);

            // Assert
            result.Should().Be(expectedValue);
            _mockLogger.AssertLogged(LogLevel.Debug, 1, "Cache hit: {Key}", key);
        }

        [Fact]
        public void Get_NonExistingKey_ReturnsDefault()
        {
            // Arrange
            var key = "nonExistentKey";
            _mockMemoryCache.TryGetValue(key, out Arg.Any<object>())
                .Returns(x => { x[1] = null; return false; });

            // Act
            var result = _sut.Get<string>(key);

            // Assert
            result.Should().BeNull();
            _mockLogger.AssertLogged(LogLevel.Debug, 1, "Cache miss: {Key}", key);
        }

        [Fact]
        public void Get_NullKey_ReturnsDefault()
        {
            // Act
            var result = _sut.Get<string>(null);

            // Assert
            result.Should().BeNull();
            _mockMemoryCache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<string>(), out Arg.Any<object>());
        }

        [Fact]
        public void Get_EmptyKey_ReturnsDefault()
        {
            // Act
            var result = _sut.Get<string>("");

            // Assert
            result.Should().BeNull();
            _mockMemoryCache.DidNotReceiveWithAnyArgs().TryGetValue(Arg.Any<string>(), out Arg.Any<object>());
        }

        [Fact]
        public void Set_ValueWithDefaultExpiration_AddsToCache()
        {
            // Arrange
            // Note: IMemoryCache.Set(...) is a non-mockable extension method that internally
            // calls CreateEntry(key) and sets properties on the returned ICacheEntry, so we
            // verify against CreateEntry/ICacheEntry directly instead.
            var key = "newKey";
            var value = "newValue";
            var entry = Substitute.For<ICacheEntry>();
            _mockMemoryCache.CreateEntry(key).Returns(entry);

            // Act
            _sut.Set(key, value);

            // Assert
            _mockMemoryCache.Received(1).CreateEntry(key);
            entry.Received(1).Value = value;
            entry.SlidingExpiration.Should().Be(TimeSpan.FromHours(1));
            _mockLogger.AssertLogged(LogLevel.Debug, 1, "Cache set: {Key} (expires in {Expiration}s)", key, TimeSpan.FromHours(1).TotalSeconds);
        }

        [Fact]
        public void Set_ValueWithCustomExpiration_AddsToCache()
        {
            // Arrange
            var key = "customKey";
            var value = 123;
            var customExpiration = TimeSpan.FromMinutes(5);
            var entry = Substitute.For<ICacheEntry>();
            _mockMemoryCache.CreateEntry(key).Returns(entry);

            // Act
            _sut.Set(key, value, customExpiration);

            // Assert
            _mockMemoryCache.Received(1).CreateEntry(key);
            entry.Received(1).Value = value;
            entry.SlidingExpiration.Should().Be(customExpiration);
            _mockLogger.AssertLogged(LogLevel.Debug, 1, "Cache set: {Key} (expires in {Expiration}s)", key, customExpiration.TotalSeconds);
        }

        [Fact]
        public void Set_NullKey_DoesNothing()
        {
            // Act
            _sut.Set<string>(null, "value");

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());
        }

        [Fact]
        public void Set_EmptyKey_DoesNothing()
        {
            // Act
            _sut.Set<string>("", "value");

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());
        }

        [Fact]
        public void Set_NullValue_DoesNothing()
        {
            // Act
            _sut.Set<string>("key", null);

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().CreateEntry(Arg.Any<string>());
        }

        [Fact]
        public void Remove_ExistingKey_RemovesFromCache()
        {
            // Arrange
            var key = "removeKey";

            // Act
            _sut.Remove(key);

            // Assert
            _mockMemoryCache.Received(1).Remove(key);
            _mockLogger.AssertLogged(LogLevel.Debug, 1, "Cache removed: {Key}", key);
        }

        [Fact]
        public void Remove_NonExistingKey_DoesNotThrow()
        {
            // Arrange
            var key = "nonExistentRemoveKey";

            // Act
            Action act = () => _sut.Remove(key);

            // Assert
            act.Should().NotThrow();
            _mockMemoryCache.Received(1).Remove(key); // Remove is called regardless
        }

        [Fact]
        public void Remove_NullKey_DoesNothing()
        {
            // Act
            _sut.Remove(null);

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().Remove(Arg.Any<string>());
        }

        [Fact]
        public void Remove_EmptyKey_DoesNothing()
        {
            // Act
            _sut.Remove("");

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().Remove(Arg.Any<string>());
        }

        [Fact]
        public void RemoveByPattern_RemovesMatchingKeys()
        {
            // Arrange
            // Simulate the internal _keyTimestamps tracking
            var cacheServiceWithTracking = new CacheService(_mockMemoryCache, _mockLogger);

            cacheServiceWithTracking.Set("prefix:key1", "value1");
            cacheServiceWithTracking.Set("prefix:key2", "value2");
            cacheServiceWithTracking.Set("other:key3", "value3");

            // Act
            cacheServiceWithTracking.RemoveByPattern("prefix:*");

            // Assert
            _mockMemoryCache.Received(1).Remove("prefix:key1");
            _mockMemoryCache.Received(1).Remove("prefix:key2");
            _mockMemoryCache.DidNotReceive().Remove("other:key3");
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Cache cleared for pattern: {Pattern} ({Count} keys)", "prefix:*", 2);
        }
        
        [Fact]
        public void RemoveByPattern_NoMatchingKeys_DoesNothing()
        {
            // Arrange
            var cacheServiceWithTracking = new CacheService(_mockMemoryCache, _mockLogger);
            cacheServiceWithTracking.Set("key1", "value1");

            // Act
            cacheServiceWithTracking.RemoveByPattern("nonexistent:*");

            // Assert
            _mockMemoryCache.DidNotReceive().Remove(Arg.Any<string>()); // No calls to remove
            _mockLogger.AssertLogged(LogLevel.Information, 1, "Cache cleared for pattern: {Pattern} ({Count} keys)", "nonexistent:*", 0);
        }

        [Fact]
        public void RemoveByPattern_NullPattern_DoesNothing()
        {
            // Act
            _sut.RemoveByPattern(null);

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().Remove(Arg.Any<string>());
        }

        [Fact]
        public void RemoveByPattern_EmptyPattern_DoesNothing()
        {
            // Act
            _sut.RemoveByPattern("");

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().Remove(Arg.Any<string>());
        }

        [Fact]
        public void Clear_RemovesAllKeys()
        {
            // Arrange
            // Need to set up _keyTimestamps internally in a way that can be tested.
            // For this test, we'll manually add some keys to the internal dictionary for the mock.
            // In a real scenario, these would be added via Set method calls.
            var cacheServiceWithTracking = new CacheService(_mockMemoryCache, _mockLogger);
            cacheServiceWithTracking.Set("keyA", "valueA");
            cacheServiceWithTracking.Set("keyB", "valueB");

            // Act
            cacheServiceWithTracking.Clear();

            // Assert
            _mockMemoryCache.Received(1).Remove("keyA");
            _mockMemoryCache.Received(1).Remove("keyB");
            _mockLogger.Received(1).LogWarning("Cache cleared (all entries removed)");
        }
        
        [Fact]
        public void Clear_EmptyCache_DoesNothing()
        {
            // Arrange - Cache is initially empty (no sets called on this _sut instance)

            // Act
            _sut.Clear();

            // Assert
            _mockMemoryCache.DidNotReceiveWithAnyArgs().Remove(Arg.Any<string>()); // No calls to remove
            _mockLogger.Received(1).LogWarning("Cache cleared (all entries removed)");
        }
    }
}
