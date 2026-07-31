using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class TenantContextHelperTests
    {
        private readonly TenantContextHelper _helper;

        public TenantContextHelperTests()
        {
            _helper = new TenantContextHelper(new LoggerFactory().CreateLogger<TenantContextHelper>());
        }

        [Fact]
        public void SetTenantContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.Equal(context, _helper.GetTenantContext());
        }

        [Fact]
        public void SetTenantContext_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _helper.SetTenantContext(null));
        }

        [Fact]
        public void GetTenantContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.Equal(context, _helper.GetTenantContext());
        }

        [Fact]
        public void GetTenantContext_NoContext_ReturnsNull()
        {
            Assert.Null(_helper.GetTenantContext());
        }

        [Fact]
        public void HasTenantContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.True(_helper.HasTenantContext());
        }

        [Fact]
        public void HasTenantContext_NoContext_ReturnsFalse()
        {
            Assert.False(_helper.HasTenantContext());
        }

        [Fact]
        public void GetCurrentTenantId_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.Equal(context.TenantId, _helper.GetCurrentTenantId());
        }

        [Fact]
        public void GetCurrentTenantId_NoContext_ReturnsNull()
        {
            Assert.Null(_helper.GetCurrentTenantId());
        }

        [Fact]
        public void ClearTenantContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            _helper.ClearTenantContext();
            Assert.Null(_helper.GetTenantContext());
        }

        [Fact]
        public void ValidateTenantContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.True(_helper.ValidateTenantContext(context.TenantId));
        }

        [Fact]
        public void ValidateTenantContext_NullTenantId_ReturnsFalse()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.False(_helper.ValidateTenantContext(null));
        }

        [Fact]
        public void ValidateTenantContext_MismatchedTenantId_ReturnsFalse()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            Assert.False(_helper.ValidateTenantContext("tenant2"));
        }

        [Fact]
        public void CreateScope_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            var scope = _helper.CreateScope(context.TenantId);
            Assert.NotNull(scope);
            Assert.Equal(context, _helper.GetTenantContext());
            scope.Dispose();
            Assert.Null(_helper.GetTenantContext());
        }

        [Fact]
        public void CreateScope_NullTenantId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _helper.CreateScope(null));
        }

        [Fact]
        public void GetContextMetadata_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            var metadata = _helper.GetContextMetadata();
            Assert.NotNull(metadata);
            Assert.Equal(context.TenantId, metadata["TenantId"]);
        }

        [Fact]
        public void GetContextMetadata_NoContext_ReturnsEmptyDictionary()
        {
            Assert.Empty(_helper.GetContextMetadata());
        }

        [Fact]
        public void EnrichErrorWithContext_HappyPath()
        {
            var context = new TenantContext { TenantId = "tenant1" };
            _helper.SetTenantContext(context);
            var errorMessage = "Error message";
            var enrichedMessage = _helper.EnrichErrorWithContext(errorMessage);
            Assert.NotNull(enrichedMessage);
            Assert.Contains(context.TenantId, enrichedMessage);
        }

        [Fact]
        public void EnrichErrorWithContext_NoContext_ReturnsOriginalErrorMessage()
        {
            var errorMessage = "Error message";
            var enrichedMessage = _helper.EnrichErrorWithContext(errorMessage);
            Assert.Equal(errorMessage, enrichedMessage);
        }
    }
}
