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
    // Generates and tracks correlation IDs for distributed request tracing
    public sealed class RequestCorrelationIdGenerator {
        private static readonly AsyncLocal<string> _currentCorrelationId =
            new AsyncLocal<string>();

        private static readonly AsyncLocal<List<string>> _correlationChain =
            new AsyncLocal<List<string>>();

        // Generates a new correlation ID
        public static string GenerateCorrelationId()
        {
            // Format: tenant_timestamp_guid
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"req_{timestamp}_{guid}";
        }

        // Sets the current correlation ID
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

        // Gets the current correlation ID
        public static string GetCorrelationId()
        {
            if (string.IsNullOrEmpty(_currentCorrelationId.Value))
            {
                var newId = GenerateCorrelationId();
                SetCorrelationId(newId);
            }

            return _currentCorrelationId.Value;
        }

        // Checks if a correlation ID is set
        public static bool HasCorrelationId()
        {
            return !string.IsNullOrEmpty(_currentCorrelationId.Value);
        }

        // Gets the correlation chain for tracing
        public static List<string> GetCorrelationChain()
        {
            return _correlationChain.Value ?? new List<string>();
        }

        // Clears the correlation ID
        public static void ClearCorrelationId()
        {
            _currentCorrelationId.Value = null;
            _correlationChain.Value = null;
        }

        // Creates a scoped correlation context
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
