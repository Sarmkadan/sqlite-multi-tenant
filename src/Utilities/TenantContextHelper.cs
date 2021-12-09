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
    /// <summary>
    /// Helper class for managing tenant context in multi-tenant operations.
    /// </summary>
    public sealed class TenantContextHelper
    {
        private readonly ILogger<TenantContextHelper> _logger;
        private static readonly AsyncLocal<TenantContext> _currentContext = new AsyncLocal<TenantContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantContextHelper"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TenantContextHelper(ILogger<TenantContextHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the current tenant context.
        /// </summary>
        /// <param name="context">The tenant context to set.</param>
        public void SetTenantContext(TenantContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _currentContext.Value = context;
            _logger.LogDebug("Tenant context set: {TenantId}", context.TenantId);
        }

        /// <summary>
        /// Gets the current tenant context.
        /// </summary>
        /// <returns>The current tenant context, or null if no context is set.</returns>
        public TenantContext GetTenantContext()
        {
            var context = _currentContext.Value;
            if (context is null)
            {
                _logger.LogWarning("No tenant context is set");
            }

            return context;
        }

        /// <summary>
        /// Checks if a tenant context is set.
        /// </summary>
        /// <returns>true if a tenant context is set, false otherwise.</returns>
        public bool HasTenantContext()
        {
            return _currentContext.Value is not null;
        }

        /// <summary>
        /// Gets the current tenant ID.
        /// </summary>
        /// <returns>The current tenant ID, or null if no context is set.</returns>
        public string GetCurrentTenantId()
        {
            return _currentContext.Value?.TenantId;
        }

        /// <summary>
        /// Clears the current tenant context.
        /// </summary>
        public void ClearTenantContext()
        {
            _currentContext.Value = null;
            _logger.LogDebug("Tenant context cleared");
        }

        /// <summary>
        /// Validates the tenant context.
        /// </summary>
        /// <param name="expectedTenantId">The expected tenant ID, or null to ignore.</param>
        /// <returns>true if the tenant context is valid, false otherwise.</returns>
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

        /// <summary>
        /// Creates a scoped context for a specific operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID to use for the scoped context.</param>
        /// <param name="userId">The user ID to use for the scoped context, or null to ignore.</param>
        /// <returns>An IDisposable instance that will restore the previous context when disposed.</returns>
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

        /// <summary>
        /// Gets metadata for the current tenant.
        /// </summary>
        /// <returns>A dictionary containing metadata for the current tenant.</returns>
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

        /// <summary>
        /// Enriches an error with tenant context information.
        /// </summary>
        /// <param name="errorMessage">The error message to enrich.</param>
        /// <returns>The enriched error message.</returns>
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

            /// <summary>
            /// Initializes a new instance of the <see cref="TenantContextScope"/> class.
            /// </summary>
            /// <param name="helper">The TenantContextHelper instance.</param>
            /// <param name="previousContext">The previous tenant context, or null to clear the current context.</param>
            public TenantContextScope(TenantContextHelper helper, TenantContext previousContext)
            {
                _helper = helper;
                _previousContext = previousContext;
            }

            /// <summary>
            /// Disposes the scope and restores the previous context.
            /// </summary>
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
