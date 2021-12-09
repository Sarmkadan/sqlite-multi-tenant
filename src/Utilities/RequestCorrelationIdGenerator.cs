#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Generates and tracks correlation IDs for distributed request tracing.
    /// </summary>
    public sealed class RequestCorrelationIdGenerator
    {
        private static readonly AsyncLocal<string> _currentCorrelationId =
            new AsyncLocal<string>();

        private static readonly AsyncLocal<List<string>> _correlationChain =
            new AsyncLocal<List<string>>();

        /// <summary>
        /// Generates a new correlation ID in the format "tenant_timestamp_guid".
        /// </summary>
        /// <returns>A new correlation ID.</returns>
        public static string GenerateCorrelationId()
        {
            // Format: tenant_timestamp_guid
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"req_{timestamp}_{guid}";
        }

        /// <summary>
        /// Sets the current correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to set.</param>
        /// <exception cref="ArgumentException">Thrown when the correlation ID is empty.</exception>
        public static void SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

            _currentCorrelationId.Value = correlationId;

            if (_correlationChain.Value is null)
            {
                _correlationChain.Value = new List<string>();
            }

            _correlationChain.Value.Add(correlationId);
        }

        /// <summary>
        /// Gets the current correlation ID.
        /// If no correlation ID is set, a new one is generated.
        /// </summary>
        /// <returns>The current correlation ID.</returns>
        public static string GetCorrelationId()
        {
            if (string.IsNullOrEmpty(_currentCorrelationId.Value))
            {
                var newId = GenerateCorrelationId();
                SetCorrelationId(newId);
            }

            return _currentCorrelationId.Value;
        }

        /// <summary>
        /// Checks if a correlation ID is set.
        /// </summary>
        /// <returns>True if a correlation ID is set, false otherwise.</returns>
        public static bool HasCorrelationId()
        {
            return !string.IsNullOrEmpty(_currentCorrelationId.Value);
        }

        /// <summary>
        /// Gets the correlation chain for tracing.
        /// </summary>
        /// <returns>The correlation chain.</returns>
        public static List<string> GetCorrelationChain()
        {
            return _correlationChain.Value ?? new List<string>();
        }

        /// <summary>
        /// Clears the correlation ID.
        /// </summary>
        public static void ClearCorrelationId()
        {
            _currentCorrelationId.Value = null;
            _correlationChain.Value = null;
        }

        /// <summary>
        /// Creates a scoped correlation context.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>A disposable scope that restores the previous correlation ID when disposed.</returns>
        public static IDisposable CreateScope(string tenantId)
        {
            var correlationId = GenerateCorrelationId();
            var previousId = _currentCorrelationId.Value;

            SetCorrelationId(correlationId);

            return new CorrelationIdScope(previousId);
        }

        private class CorrelationIdScope : IDisposable
        {
            private readonly string _previousCorrelationId;
            private bool _disposed;

            public CorrelationIdScope(string previousCorrelationId)
            {
                _previousCorrelationId = previousCorrelationId;
            }

            public void Dispose()
            {
                if (_disposed) return;

                if (!string.IsNullOrEmpty(_previousCorrelationId))
                {
                    _currentCorrelationId.Value = _previousCorrelationId;
                }
                else
                {
                    ClearCorrelationId();
                }

                _disposed = true;
            }
        }
    }
}
