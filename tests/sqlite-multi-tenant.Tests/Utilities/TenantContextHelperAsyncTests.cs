#nullable enable

using System;
using System.Threading.Tasks;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Utilities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SqliteMultiTenant.Tests.Utilities
{
    /// <summary>
    /// Tests for async-safe tenant context operations
    /// </summary>
    public class TenantContextHelperAsyncTests
    {
        private readonly TenantContextHelper _helper;

        public TenantContextHelperAsyncTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddFilter(level => level >= Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            _helper = new TenantContextHelper(loggerFactory.CreateLogger<TenantContextHelper>());
        }

        [Fact]
        public void CreateScope_ShouldSaveAndRestorePreviousContext()
        {
            // Arrange
            var initialContext = new TenantContext { TenantId = "initial-tenant" };
            _helper.SetTenantContext(initialContext);

            var newContext = new TenantContext { TenantId = "new-tenant" };

            // Act
            TenantContext? capturedContext = null;
            using (var scope = _helper.CreateScope("scoped-tenant"))
            {
                capturedContext = _helper.GetTenantContext();
                Assert.Equal("scoped-tenant", capturedContext.TenantId);
            }

            // Assert
            var restoredContext = _helper.GetTenantContext();
            Assert.Equal("initial-tenant", restoredContext.TenantId);
        }

        [Fact]
        public async Task ExecuteInTenantContext_WithAsyncAction_ShouldRestoreContextAfterAsyncOperation()
        {
            // Arrange
            var initialContext = new TenantContext { TenantId = "initial-tenant" };
            _helper.SetTenantContext(initialContext);

            // Act - run async operation
            await _helper.ExecuteInTenantContextAsync("async-tenant", async () =>
            {
                var context = _helper.GetTenantContext();
                Assert.Equal("async-tenant", context.TenantId);

                // Simulate async work
                await Task.Delay(10);

                context = _helper.GetTenantContext();
                Assert.Equal("async-tenant", context.TenantId);
            });

            // Assert
            var restoredContext = _helper.GetTenantContext();
            Assert.Equal("initial-tenant", restoredContext.TenantId);
        }

        [Fact]
        public async Task NestedScopes_ShouldProperlyRestoreContexts()
        {
            // Arrange
            var outerContext = new TenantContext { TenantId = "outer-tenant" };
            _helper.SetTenantContext(outerContext);

            // Act - create nested scopes
            using (var scope1 = _helper.CreateScope("middle-tenant"))
            {
                Assert.Equal("middle-tenant", _helper.GetTenantContext().TenantId);

                using (var scope2 = _helper.CreateScope("inner-tenant"))
                {
                    Assert.Equal("inner-tenant", _helper.GetTenantContext().TenantId);
                }

                // After inner scope disposed, should restore to middle
                Assert.Equal("middle-tenant", _helper.GetTenantContext().TenantId);
            }

            // After middle scope disposed, should restore to outer
            Assert.Equal("outer-tenant", _helper.GetTenantContext().TenantId);
        }

        [Fact]
        public async Task InterleavedAsyncFlows_ShouldNotLeakContexts()
        {
            // Arrange - set initial context
            var contextA = new TenantContext { TenantId = "tenant-a" };
            _helper.SetTenantContext(contextA);

            var results = new System.Collections.Generic.List<string>();

            // Act - run two async flows interleaved
            var task1 = RunFlowAsync("flow-1", "tenant-1", results);
            var task2 = RunFlowAsync("flow-2", "tenant-2", results);

            await Task.WhenAll(task1, task2);

            // Assert - both flows should have correct tenant contexts
            Assert.Contains("flow-1:tenant-1", results);
            Assert.Contains("flow-2:tenant-2", results);

            // Final context should be restored to initial
            var finalContext = _helper.GetTenantContext();
            Assert.Equal("tenant-a", finalContext.TenantId);
        }

        [Fact]
        public void IsCurrentTenant_ShouldWorkCorrectly()
        {
            // Arrange
            var context = new TenantContext { TenantId = "current-tenant" };
            _helper.SetTenantContext(context);

            // Act & Assert
            Assert.True(_helper.IsCurrentTenant("current-tenant"));
            Assert.False(_helper.IsCurrentTenant("other-tenant"));
        }

        [Fact]
        public void GetRequiredTenantId_ShouldThrowWhenNoContext()
        {
            // Arrange
            _helper.ClearTenantContext();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _helper.GetRequiredTenantId());
        }

        [Fact]
        public void GetRequiredTenantContext_ShouldThrowWhenNoContext()
        {
            // Arrange
            _helper.ClearTenantContext();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _helper.GetRequiredTenantContext());
        }

        private async Task RunFlowAsync(string flowName, string tenantId, System.Collections.Generic.List<string> results)
        {
            using (var scope = _helper.CreateScope(tenantId))
            {
                var context = _helper.GetTenantContext();
                results.Add($"{flowName}:{context.TenantId}");

                // Simulate async work that might switch threads
                await Task.Delay(5);

                context = _helper.GetTenantContext();
                results.Add($"{flowName}:{context.TenantId}");
            }
        }
    }
}
