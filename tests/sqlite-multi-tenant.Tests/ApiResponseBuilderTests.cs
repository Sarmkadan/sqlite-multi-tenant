using System;
using System.Collections.Generic;
using System.Net;
using SqliteMultiTenant.Api;
using SqliteMultiTenant.Api.Responses;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class ApiResponseBuilderTests
    {
        [Fact]
        public void Build_WithDataMessageAndSuccess_ShouldReturnSuccessfulResponse()
        {
            // Arrange
            var builder = new ApiResponseBuilder<string>()
                .WithData("sample data")
                .WithMessage("operation succeeded")
                .Success();

            // Act
            ApiResponse<string> response = builder.Build();

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal("sample data", response.Data);
            Assert.Equal("operation succeeded", response.Message);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void Created_WithoutExplicitMessage_ShouldSetDefaultMessageAndStatusCreated()
        {
            // Arrange
            var builder = new ApiResponseBuilder<string>()
                .Created();

            // Act
            ApiResponse<string> response = builder.Build();

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal((int)HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("Resource created successfully", response.Message);
        }

        [Fact]
        public void AddError_ShouldPopulateErrorsDictionaryWithCodeAndMessage()
        {
            // Arrange
            var builder = new ApiResponseBuilder<object>()
                .AddError("validation failed", code: "VALIDATION_ERROR", field: "Name")
                .Failure();

            // Act
            ApiResponse<object> response = builder.Build();

            // Assert
            Assert.False(response.IsSuccess);
            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.ContainsKey("VALIDATION_ERROR"));
            Assert.Equal("validation failed", response.Errors["VALIDATION_ERROR"]);
            Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public void AddErrors_WithEmptyCollection_ShouldNotThrowAndLeaveErrorsNull()
        {
            // Arrange
            var emptyErrors = new List<ApiError>();
            var builder = new ApiResponseBuilder<object>()
                .AddErrors(emptyErrors)
                .Success();

            // Act
            ApiResponse<object> response = builder.Build();

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Null(response.Errors);
        }

        [Fact]
        public void Failure_WithoutExplicitStatus_ShouldDefaultToBadRequest()
        {
            // Arrange
            var builder = new ApiResponseBuilder<string>()
                .Failure();

            // Act
            ApiResponse<string> response = builder.Build();

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public void WithStatusCode_ShouldOverrideDefaultStatus()
        {
            // Arrange
            var builder = new ApiResponseBuilder<string>()
                .WithStatusCode(HttpStatusCode.Accepted)
                .Success();

            // Act
            ApiResponse<string> response = builder.Build();

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal((int)HttpStatusCode.Accepted, response.StatusCode);
        }

        [Fact]
        public void WithData_NullValue_ShouldAllowNullDataInResponse()
        {
            // Arrange
            var builder = new ApiResponseBuilder<string>()
                .WithData(null)
                .Success();

            // Act
            ApiResponse<string> response = builder.Build();

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Null(response.Data);
        }
    }
}
