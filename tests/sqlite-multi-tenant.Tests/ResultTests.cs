using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Tests
{
    public class ResultTests
    {
        [Fact]
        public void Ok_SetsSuccessTrueAndData()
        {
            // Arrange
            var expectedData = "test data";
            var expectedMessage = "test message";

            // Act
            var result = Result<string>.Ok(expectedData, expectedMessage);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedData, result.Data);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Empty(result.Errors);
            Assert.Null(result.Metadata);
        }

        [Fact]
        public void Ok_WithNullData_SetsSuccessTrue()
        {
            // Act
            var result = Result<string>.Ok(null, null);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Data);
            Assert.Null(result.Message);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Fail_WithSingleError_SetsSuccessFalseAndErrors()
        {
            // Arrange
            var errorMessage = "test error";

            // Act
            var result = Result<string>.Fail(errorMessage);

            // Assert
            Assert.False(result.Success);
            Assert.Single(result.Errors);
            Assert.Equal(errorMessage, result.Errors[0]);
            Assert.Null(result.Data);
            Assert.Null(result.Message);
            Assert.Null(result.Metadata);
        }

        [Fact]
        public void Fail_WithErrorList_SetsSuccessFalseAndErrors()
        {
            // Arrange
            var errors = new List<string> { "error1", "error2" };

            // Act
            var result = Result<string>.Fail(errors);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal(errors, result.Errors);
            Assert.Null(result.Data);
            Assert.Null(result.Message);
            Assert.Null(result.Metadata);
        }

        [Fact]
        public void Metadata_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = Result<string>.Ok("data");
            var metadata = new ResultMetadata
            {
                TraceId = "trace-123",
                StatusCode = 200
            };

            // Act
            result.Metadata = metadata;

            // Assert
            Assert.Same(metadata, result.Metadata);
            Assert.Equal("trace-123", result.Metadata?.TraceId);
            Assert.Equal(200, result.Metadata?.StatusCode);
        }

        [Fact]
        public void PaginatedResult_Ok_SetsPropertiesCorrectly()
        {
            // Arrange
            var items = new List<int> { 1, 2, 3 };
            int pageNumber = 2;
            int pageSize = 10;
            int totalCount = 25;

            // Act
            var result = PaginatedResult<int>.Ok(items, pageNumber, pageSize, totalCount);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(items, result.Items);
            Assert.Equal(pageNumber, result.Pagination.PageNumber);
            Assert.Equal(pageSize, result.Pagination.PageSize);
            Assert.Equal(totalCount, result.Pagination.TotalCount);
            Assert.Equal(3, result.Pagination.TotalPages); // Ceiling(25/10) = 3
            Assert.True(result.Pagination.HasPreviousPage); // PageNumber > 1? 2 > 1 -> true
            Assert.True(result.Pagination.HasNextPage); // PageNumber < TotalPages? 2 < 3 -> true
            Assert.Null(result.Message);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void PaginatedResult_Fail_WithError_SetsSuccessFalseAndErrors()
        {
            // Arrange
            var errorMessage = "pagination failed";

            // Act
            var result = PaginatedResult<int>.Fail(errorMessage);

            // Assert
            Assert.False(result.Success);
            Assert.Single(result.Errors);
            Assert.Equal(errorMessage, result.Errors[0]);
            Assert.Empty(result.Items);
            Assert.Null(result.Message);
        }
    }
}