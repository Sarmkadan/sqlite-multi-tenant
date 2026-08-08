using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using SqliteMultiTenant.Integration;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class HttpClientWrapperExtensionsTests
    {
        [Fact]
        public void AddJsonAcceptHeader_ThrowsArgumentNullException_WhenClientIsNull()
        {
            // Arrange
            IHttpClientWrapper client = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => HttpClientWrapperExtensions.AddJsonAcceptHeader(client));
        }

        [Fact]
        public void AddJsonAcceptHeader_AddsAcceptHeader_WhenClientIsValid()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();

            // Act
            HttpClientWrapperExtensions.AddJsonAcceptHeader(client);

            // Assert
            client.Received(1).AddDefaultHeader("Accept", "application/json");
        }

        [Fact]
        public async Task GetListAsync_ThrowsArgumentNullException_WhenClientIsNull()
        {
            // Arrange
            IHttpClientWrapper client = null;
            string requestUri = "https://example.com";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => HttpClientWrapperExtensions.GetListAsync<string>(client, requestUri));
        }

        [Fact]
        public async Task GetListAsync_ThrowsArgumentNullException_WhenRequestUriIsNull()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => HttpClientWrapperExtensions.GetListAsync<string>(client, requestUri));
        }

        [Fact]
        public async Task GetListAsync_ThrowsArgumentException_WhenRequestUriIsEmpty()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = string.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => HttpClientWrapperExtensions.GetListAsync<string>(client, requestUri));
        }

        [Fact]
        public async Task GetListAsync_ReturnsList_WhenClientReturnsValidResponse()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = "https://example.com/api/data";
            var expectedList = new List<string> { "item1", "item2" }.AsReadOnly();

            client.GetAsync<IReadOnlyList<string>>(requestUri).Returns(expectedList);

            // Act
            var result = await HttpClientWrapperExtensions.GetListAsync<string>(client, requestUri);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedList, result);
            await client.Received(1).GetAsync<IReadOnlyList<string>>(requestUri);
        }

        [Fact]
        public async Task GetListAsync_ThrowsInvalidOperationException_WhenClientReturnsNull()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = "https://example.com/api/data";

            client.GetAsync<IReadOnlyList<string>>(requestUri).Returns((IReadOnlyList<string>)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => HttpClientWrapperExtensions.GetListAsync<string>(client, requestUri));
        }

        [Fact]
        public async Task DeleteIfExistsAsync_ThrowsArgumentNullException_WhenClientIsNull()
        {
            // Arrange
            IHttpClientWrapper client = null;
            string requestUri = "https://example.com";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => HttpClientWrapperExtensions.DeleteIfExistsAsync(client, requestUri));
        }

        [Fact]
        public async Task DeleteIfExistsAsync_ThrowsArgumentNullException_WhenRequestUriIsNull()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => HttpClientWrapperExtensions.DeleteIfExistsAsync(client, requestUri));
        }

        [Fact]
        public async Task DeleteIfExistsAsync_ThrowsArgumentException_WhenRequestUriIsEmpty()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = string.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => HttpClientWrapperExtensions.DeleteIfExistsAsync(client, requestUri));
        }

        [Fact]
        public async Task DeleteIfExistsAsync_ReturnsTrue_WhenDeleteSucceeds()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = "https://example.com/api/resource";

            client.DeleteAsync(requestUri).Returns(true);

            // Act
            var result = await HttpClientWrapperExtensions.DeleteIfExistsAsync(client, requestUri);

            // Assert
            Assert.True(result);
            await client.Received(1).DeleteAsync(requestUri);
        }

        [Fact]
        public async Task DeleteIfExistsAsync_ReturnsFalse_WhenDeleteFails()
        {
            // Arrange
            var client = Substitute.For<IHttpClientWrapper>();
            string requestUri = "https://example.com/api/resource";

            client.DeleteAsync(requestUri).Returns(false);

            // Act
            var result = await HttpClientWrapperExtensions.DeleteIfExistsAsync(client, requestUri);

            // Assert
            Assert.False(result);
            await client.Received(1).DeleteAsync(requestUri);
        }
    }
}