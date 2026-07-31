using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Unit tests for <see cref="RequestCorrelationIdGenerator"/>.
    /// </summary>
    public class RequestCorrelationIdGeneratorTests : IDisposable
    {
        // Ensure a clean static state for each test.
        public RequestCorrelationIdGeneratorTests()
        {
            RequestCorrelationIdGenerator.ClearCorrelationId();
        }

        public void Dispose()
        {
            // Clean up after each test as well.
            RequestCorrelationIdGenerator.ClearCorrelationId();
        }

        [Fact]
        public void GenerateCorrelationId_ReturnsNonEmptyAndMatchesPattern()
        {
            // Act
            var id = RequestCorrelationIdGenerator.GenerateCorrelationId();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(id));

            // Expected pattern: req_yyyyMMddHHmmssffff_8hex
            var pattern = @"^req_\d{17}_[0-9a-fA-F]{8}$";
            Assert.Matches(pattern, id);
        }

        [Fact]
        public void SetAndGetCorrelationId_Workflow()
        {
            var expected = "custom-id-123";

            // Act
            RequestCorrelationIdGenerator.SetCorrelationId(expected);
            var actual = RequestCorrelationIdGenerator.GetCorrelationId();

            // Assert
            Assert.Equal(expected, actual);
            Assert.True(RequestCorrelationIdGenerator.HasCorrelationId());
        }

        [Fact]
        public void SetCorrelationId_EmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                RequestCorrelationIdGenerator.SetCorrelationId(string.Empty));

            Assert.Equal("Correlation ID cannot be empty (Parameter 'correlationId')", ex.Message);
        }

        [Fact]
        public void GetCorrelationId_WhenNotSet_GeneratesAndSetsId()
        {
            // Ensure no ID is present
            Assert.False(RequestCorrelationIdGenerator.HasCorrelationId());

            // Act
            var id = RequestCorrelationIdGenerator.GetCorrelationId();

            // Assert
            Assert.True(RequestCorrelationIdGenerator.HasCorrelationId());
            Assert.False(string.IsNullOrWhiteSpace(id));
        }

        [Fact]
        public void GetCorrelationChain_AccumulatesIds()
        {
            // Arrange
            RequestCorrelationIdGenerator.SetCorrelationId("first");
            RequestCorrelationIdGenerator.SetCorrelationId("second");

            // Act
            var chain = RequestCorrelationIdGenerator.GetCorrelationChain();

            // Assert
            Assert.Equal(2, chain.Count);
            Assert.Equal("first", chain[0]);
            Assert.Equal("second", chain[1]);
        }

        [Fact]
        public void ClearCorrelationId_ResetsState()
        {
            // Arrange
            RequestCorrelationIdGenerator.SetCorrelationId("temp");
            Assert.True(RequestCorrelationIdGenerator.HasCorrelationId());

            // Act
            RequestCorrelationIdGenerator.ClearCorrelationId();

            // Assert
            Assert.False(RequestCorrelationIdGenerator.HasCorrelationId());
            Assert.Empty(RequestCorrelationIdGenerator.GetCorrelationChain());
        }

        [Fact]
        public void CreateScope_RestoresPreviousIdOnDispose()
        {
            // Arrange
            RequestCorrelationIdGenerator.SetCorrelationId("original");
            var originalId = RequestCorrelationIdGenerator.GetCorrelationId();

            // Act
            using (RequestCorrelationIdGenerator.CreateScope("tenant"))
            {
                var scopedId = RequestCorrelationIdGenerator.GetCorrelationId();
                Assert.NotEqual(originalId, scopedId);
                Assert.True(RequestCorrelationIdGenerator.HasCorrelationId());
            }

            // After disposal, the original ID should be restored.
            var afterDisposeId = RequestCorrelationIdGenerator.GetCorrelationId();
            Assert.Equal(originalId, afterDisposeId);
        }

        [Fact]
        public void CreateScope_WhenNoPreviousId_ClearsOnDispose()
        {
            // Ensure clean state
            Assert.False(RequestCorrelationIdGenerator.HasCorrelationId());

            // Act
            using (RequestCorrelationIdGenerator.CreateScope("tenant"))
            {
                Assert.True(RequestCorrelationIdGenerator.HasCorrelationId());
            }

            // After disposal, there should be no correlation ID.
            Assert.False(RequestCorrelationIdGenerator.HasCorrelationId());
        }
    }
}
