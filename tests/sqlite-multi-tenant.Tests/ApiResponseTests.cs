using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Tests
{
    public class ApiResponseTests
    {
        [Fact]
        public void Success_ReturnsCorrectResponse()
        {
            // Arrange
            var data = "hello world";

            // Act
            var response = ApiResponse<string>.Success(data, "All good");

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.True(response.IsSuccess);
            Assert.Equal(data, response.Data);
            Assert.Equal("All good", response.Message);
            Assert.Null(response.Errors);
            Assert.True((DateTime.UtcNow - response.Timestamp) < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Created_ReturnsCorrectResponse()
        {
            // Arrange
            var data = new List<int> { 1, 2, 3 };

            // Act
            var response = ApiResponse<List<int>>.Created(data);

            // Assert
            Assert.Equal(201, response.StatusCode);
            Assert.True(response.IsSuccess);
            Assert.Same(data, response.Data);
            Assert.Equal("Created", response.Message);
            Assert.Null(response.Errors);
            Assert.True((DateTime.UtcNow - response.Timestamp) < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void BadRequest_WithErrors_ReturnsCorrectResponse()
        {
            // Arrange
            var errors = new Dictionary<string, string>
            {
                { "field1", "must not be empty" },
                { "field2", "invalid format" }
            };
            var message = "Validation failed";

            // Act
            var response = ApiResponse<object>.BadRequest(message, errors);

            // Assert
            Assert.Equal(400, response.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal(message, response.Message);
            Assert.Same(errors, response.Errors);
            Assert.Null(response.Data);
            Assert.True((DateTime.UtcNow - response.Timestamp) < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void BadRequest_NullErrors_ReturnsResponseWithoutErrors()
        {
            // Act
            var response = ApiResponse<object>.BadRequest("Missing data");

            // Assert
            Assert.Equal(400, response.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal("Missing data", response.Message);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void DefaultConstructor_InitializesProperties()
        {
            // Act
            var response = new ApiResponse<int>();

            // Assert
            Assert.Equal(0, response.StatusCode);
            Assert.False(response.IsSuccess);
            Assert.Equal(string.Empty, response.Message);
            Assert.Equal(default, response.Data);
            Assert.Null(response.Errors);
            Assert.True((DateTime.UtcNow - response.Timestamp) < TimeSpan.FromSeconds(1));
        }
    }
}
