using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SqliteMultiTenant.Integration;
using Xunit;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqliteMultiTenant.Tests
{
    public class HttpClientWrapperValidationTests
    {
        [Fact]
        public void Validate_ReturnsEmptyList_WhenHttpClientWrapperIsValid()
        {
            // Arrange
            var httpClient = new HttpClient();
            var logger = NullLogger<HttpClientWrapper>.Instance;
            var wrapper = new HttpClientWrapper(httpClient, logger);

            // Act
            var result = HttpClientWrapperValidation.Validate(wrapper);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_ThrowsArgumentNullException_WhenHttpClientWrapperIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientWrapperValidation.Validate(null));
        }

        [Fact]
        public void IsValid_ReturnsTrue_WhenHttpClientWrapperIsValid()
        {
            // Arrange
            var httpClient = new HttpClient();
            var logger = NullLogger<HttpClientWrapper>.Instance;
            var wrapper = new HttpClientWrapper(httpClient, logger);

            // Act
            var result = HttpClientWrapperValidation.IsValid(wrapper);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenHttpClientWrapperIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientWrapperValidation.IsValid(null));
        }

        [Fact]
        public void EnsureValid_ThrowsNoException_WhenHttpClientWrapperIsValid()
        {
            // Arrange
            var httpClient = new HttpClient();
            var logger = NullLogger<HttpClientWrapper>.Instance;
            var wrapper = new HttpClientWrapper(httpClient, logger);

            // Act
            var exception = Record.Exception(() => HttpClientWrapperValidation.EnsureValid(wrapper));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void EnsureValid_ThrowsArgumentException_WhenHttpClientWrapperIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientWrapperValidation.EnsureValid(null));
        }

        [Fact]
        public void ValidateUrl_ReturnsEmptyList_WhenUrlIsValid()
        {
            // Arrange
            var url = "https://example.com/api";

            // Act
            var result = HttpClientWrapperValidation.ValidateUrl(url);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateUrl_ReturnsErrorList_WhenUrlIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientWrapperValidation.ValidateUrl(null));
        }

        [Fact]
        public void ValidateUrl_ReturnsErrorList_WhenUrlIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                HttpClientWrapperValidation.ValidateUrl(string.Empty));
        }

        [Fact]
        public void ValidateBearerToken_ReturnsEmptyList_WhenTokenIsValid()
        {
            // Arrange
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = HttpClientWrapperValidation.ValidateBearerToken(token);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateBearerToken_ReturnsErrorList_WhenTokenIsTooShort()
        {
            // Arrange
            var token = "short";

            // Act
            var result = HttpClientWrapperValidation.ValidateBearerToken(token);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Contains("at least 10 characters"));
        }

        [Fact]
        public void ValidateHeader_ReturnsEmptyList_WhenHeaderIsValid()
        {
            // Arrange
            var name = "Content-Type";
            var value = "application/json";

            // Act
            var result = HttpClientWrapperValidation.ValidateHeader(name, value);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateHeader_ReturnsErrorList_WhenNameIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientWrapperValidation.ValidateHeader(null, "value"));
        }

        [Fact]
        public void ValidateHeader_ReturnsErrorList_WhenValueIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                HttpClientWrapperValidation.ValidateHeader("name", string.Empty));
        }

        [Fact]
        public void ValidatePayload_ReturnsEmptyList_WhenPayloadIsNotNull()
        {
            // Arrange
            var payload = new { Id = 1, Name = "Test" };

            // Act
            var result = HttpClientWrapperValidation.ValidatePayload(payload);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidatePayload_ReturnsErrorList_WhenPayloadIsNull()
        {
            // Act
            var result = HttpClientWrapperValidation.ValidatePayload(null);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Contains("Payload cannot be null"));
        }

        [Fact]
        public void ValidateResponseType_ReturnsEmptyList_WhenTypeIsValidClass()
        {
            // Act
            var result = HttpClientWrapperValidation.ValidateResponseType<string>();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateResponseType_ReturnsErrorList_WhenTypeIsValueType()
        {
            // Act
            var result = HttpClientWrapperValidation.ValidateResponseType<int>();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Contains("value type"));
        }

        [Fact]
        public void ValidateResponseType_ReturnsErrorList_WhenTypeHasNoParameterlessConstructor()
        {
            // Arrange
            var result = HttpClientWrapperValidation.ValidateResponseType<System.Uri>();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, p => p.Contains("parameterless constructor"));
        }
    }
}