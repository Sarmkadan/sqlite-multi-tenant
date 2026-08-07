using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class MultiTenantHttpClientFactoryExtensionsTests : IDisposable
    {
        private readonly MultiTenantHttpClientFactory _factory;

        public MultiTenantHttpClientFactoryExtensionsTests()
        {
            _factory = new MultiTenantHttpClientFactory(NullLogger<MultiTenantHttpClientFactory>.Instance);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        [Fact]
        public void GetOrCreateClient_ReturnsClient_WhenFactoryAndTenantIdAreValid()
        {
            // Act
            var client = MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(_factory, "tenant1");

            // Assert
            Assert.NotNull(client);
            Assert.Equal("tenant1", client.DefaultRequestHeaders.GetValues("X-Tenant-Id").FirstOrDefault());
        }

        [Fact]
        public void GetOrCreateClient_ThrowsArgumentNullException_WhenFactoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(null, "tenant1"));
        }

        [Fact]
        public void GetOrCreateClient_ThrowsArgumentNullException_WhenTenantIdIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(_factory, null));
        }

        [Fact]
        public void GetOrCreateClient_ThrowsArgumentException_WhenTenantIdIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(_factory, string.Empty));
        }

        [Fact]
        public void GetOrCreateClient_ThrowsArgumentException_WhenTenantIdIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(_factory, "   "));
        }

        [Fact]
        public void SetDefaultHeader_AddsHeader_WhenParametersAreValid()
        {
            // Act
            MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(_factory, "tenant1", "X-Custom-Header", "custom-value");

            // Assert
            var client = _factory.GetCachedClient("tenant1");
            Assert.NotNull(client);
            Assert.Equal("custom-value", client.DefaultRequestHeaders.GetValues("X-Custom-Header").FirstOrDefault());
        }

        [Fact]
        public void SetDefaultHeader_ThrowsArgumentNullException_WhenFactoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(null, "tenant1", "X-Custom-Header", "value"));
        }

        [Fact]
        public void SetDefaultHeader_ThrowsArgumentNullException_WhenTenantIdIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(_factory, null, "X-Custom-Header", "value"));
        }

        [Fact]
        public void SetDefaultHeader_ThrowsArgumentNullException_WhenHeaderNameIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(_factory, "tenant1", null, "value"));
        }

        [Fact]
        public void SetDefaultHeader_ThrowsArgumentException_WhenHeaderNameIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(_factory, "tenant1", string.Empty, "value"));
        }

        [Fact]
        public void SetDefaultHeader_ThrowsArgumentException_WhenHeaderNameIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetDefaultHeader(_factory, "tenant1", "   ", "value"));
        }

        [Fact]
        public void SetTimeout_SetsTimeout_WhenParametersAreValid()
        {
            // Act
            var timeout = TimeSpan.FromSeconds(60);
            MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, "tenant1", timeout);

            // Assert
            var client = _factory.GetCachedClient("tenant1");
            Assert.NotNull(client);
            Assert.Equal(timeout, client.Timeout);
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentNullException_WhenFactoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(null, "tenant1", TimeSpan.FromSeconds(30)));
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentNullException_WhenTenantIdIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, null, TimeSpan.FromSeconds(30)));
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentException_WhenTenantIdIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, string.Empty, TimeSpan.FromSeconds(30)));
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentException_WhenTenantIdIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, "   ", TimeSpan.FromSeconds(30)));
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentOutOfRangeException_WhenTimeoutIsZero()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, "tenant1", TimeSpan.Zero));
        }

        [Fact]
        public void SetTimeout_ThrowsArgumentOutOfRangeException_WhenTimeoutIsNegative()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MultiTenantHttpClientFactoryExtensions.SetTimeout(_factory, "tenant1", TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void RefreshClient_ReturnsNewClient_WhenFactoryAndTenantIdAreValid()
        {
            // Arrange
            var firstClient = MultiTenantHttpClientFactoryExtensions.GetOrCreateClient(_factory, "tenant1");

            // Act
            var secondClient = MultiTenantHttpClientFactoryExtensions.RefreshClient(_factory, "tenant1");

            // Assert
            Assert.NotNull(secondClient);
            Assert.NotEqual(firstClient, secondClient);
            Assert.Equal("tenant1", secondClient.DefaultRequestHeaders.GetValues("X-Tenant-Id").FirstOrDefault());
        }

        [Fact]
        public void RefreshClient_ThrowsArgumentNullException_WhenFactoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.RefreshClient(null, "tenant1"));
        }

        [Fact]
        public void RefreshClient_ThrowsArgumentNullException_WhenTenantIdIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                MultiTenantHttpClientFactoryExtensions.RefreshClient(_factory, null));
        }

        [Fact]
        public void RefreshClient_ThrowsArgumentException_WhenTenantIdIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.RefreshClient(_factory, string.Empty));
        }

        [Fact]
        public void RefreshClient_ThrowsArgumentException_WhenTenantIdIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                MultiTenantHttpClientFactoryExtensions.RefreshClient(_factory, "   "));
        }
    }
}