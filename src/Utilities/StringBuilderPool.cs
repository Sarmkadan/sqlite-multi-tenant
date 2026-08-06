using System;
using System.Collections.Generic;
using System.Text;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Object pool for StringBuilder instances to reduce allocations.
    /// </summary>
    public static class StringBuilderPool
    {
        // Maximum number of StringBuilder instances to keep in the pool
        private const int MaxPooledInstances = 64;

        // Pool of StringBuilder instances
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>();

        /// <summary>
        /// Rents a StringBuilder instance from the pool, or creates a new one if none are available.
        /// </summary>
        /// <param name="capacity">Initial capacity for the StringBuilder. If 0, uses default capacity.</param>
        /// <returns>A StringBuilder instance.</returns>
        public static StringBuilder Rent(int capacity = 0)
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                {
                    var sb = _pool.Pop();
                    if (sb.Capacity < capacity)
                    {
                        // Ensure the StringBuilder has at least the requested capacity
                        sb.EnsureCapacity(capacity);
                    }
                    sb.Clear();
                    return sb;
                }
            }

            // If no pooled instance available or capacity too large, create new
            return capacity > 0 ? new StringBuilder(capacity) : new StringBuilder();
        }

        /// <summary>
        /// Returns a StringBuilder instance to the pool for reuse.
        /// </summary>
        /// <param name="sb">The StringBuilder to return to the pool.</param>
        public static void Return(StringBuilder sb)
        {
            if (sb == null)
                return;

            lock (_pool)
            {
                // Only pool if we haven't exceeded the maximum
                if (_pool.Count < MaxPooledInstances)
                {
                    sb.Clear();
                    _pool.Push(sb);
                }
            }
        }
    }
}