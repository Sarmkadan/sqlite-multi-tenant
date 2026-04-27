#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Utilities
{
    // Helper class for managing tenant context in multi-tenant operations
    public sealed class TenantContextHelper {
        private readonly ILogger<TenantContextHelper> _logger;
        private static readonly AsyncLocal<TenantContext> _currentContext = new AsyncLocal<TenantContext>();

        public TenantContextHelper(ILogger<TenantContextHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Sets the current tenant context
        public void SetTenantContext(TenantContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _currentContext.Value = context;
            _logger.LogDebug("Tenant context set: {TenantId}", context.TenantId);
        }

        // Gets the current tenant context
        public TenantContext GetTenantContext()
        {
            var context = _currentContext.Value;
            if (context is null)
            {
                _logger.LogWarning("No tenant context is set");
            }

            return context;
        }

        // Checks if a tenant context is set
        public bool HasTenantContext()
        {
            return _currentContext.Value is not null;
        }

        // Gets the current tenant ID
        public string GetCurrentTenantId()
        {
            return _currentContext.Value?.TenantId;
        }

        // Clears the current tenant context
        public void ClearTenantContext()
        {
            _currentContext.Value = null;
            _logger.LogDebug("Tenant context cleared");
        }

        // Validates tenant context
        public bool ValidateTenantContext(string expectedTenantId = null)
        {
            var context = GetTenantContext();

            if (context is null)
            {
                _logger.LogWarning("Tenant context validation failed: context is null");
                return false;
            }

            if (string.IsNullOrEmpty(context.TenantId))
            {
                _logger.LogWarning("Tenant context validation failed: TenantId is empty");
                return false;
            }

            if (!string.IsNullOrEmpty(expectedTenantId) && context.TenantId != expectedTenantId)
            {
                _logger.LogWarning("Tenant context validation failed: expected {Expected}, got {Actual}",
                    expectedTenantId, context.TenantId);
                return false;
            }

            return true;
        }

        // Creates a scoped context for a specific operation
        public IDisposable CreateScope(string tenantId, string userId = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

            var previousContext = _currentContext.Value;
            var newContext = new TenantContext
            {
                TenantId = tenantId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            SetTenantContext(newContext);

            return new TenantContextScope(this, previousContext);
        }

        // Gets metadata for the current tenant
        public Dictionary<string, object> GetContextMetadata()
        {
            var context = GetTenantContext();
            if (context is null)
                return new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                { "TenantId", context.TenantId },
                { "UserId", context.UserId },
                { "CreatedAt", context.CreatedAt },
                { "RequestId", context.RequestId }
            };
        }

        // Enriches an error with tenant context information
        public string EnrichErrorWithContext(string errorMessage)
        {
            var context = GetTenantContext();
            if (context is null)
                return errorMessage;

            return $"{errorMessage} [TenantId: {context.TenantId}]";
        }

        private class TenantContextScope : IDisposable
        {
            private readonly TenantContextHelper _helper;
            private readonly TenantContext _previousContext;
            private bool _disposed;

            public TenantContextScope(TenantContextHelper helper, TenantContext previousContext)
            {
                _helper = helper;
                _previousContext = previousContext;
            }

            public void Dispose()
            {
                if (_disposed) return;

                if (_previousContext is not null)
                {
                    _helper.SetTenantContext(_previousContext);
                }
                else
                {
                    _helper.ClearTenantContext();
                }

                _disposed = true;
            }
        }
    }
}
