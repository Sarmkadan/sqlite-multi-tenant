#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Extension methods for <see cref="TenantContextHelper"/> that provide common tenant context operations.
    /// </summary>
    public static class TenantContextHelperExtensions
    {
        /// <summary>
        /// Creates a tenant context scope with automatic tenant ID validation.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="tenantId">The tenant ID to set in the context.</param>
        /// <param name="userId">Optional user ID to associate with the context.</param>
        /// <returns>An <see cref="IDisposable"/> scope that restores the previous context when disposed.</returns>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or whitespace.</exception>
        public static IDisposable CreateValidatedScope(this TenantContextHelper helper, string tenantId, string userId = null)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

            if (helper.IsCurrentTenant(tenantId))
            {
                return new EmptyDisposable();
            }

            return helper.CreateScope(tenantId, userId);
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary>
        /// Gets the current tenant ID or throws if not available.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <returns>The current tenant ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no tenant context is set.</exception>
        public static string GetRequiredTenantId(this TenantContextHelper helper)
        {
            ArgumentNullException.ThrowIfNull(helper);

            var tenantId = helper.GetCurrentTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("Tenant context is not set. Call SetTenantContext first.");
            }

            return tenantId;
        }

        /// <summary>
        /// Executes an action within a tenant context scope, automatically restoring the previous context.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="tenantId">The tenant ID to set for the duration of the action.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="userId">Optional user ID to associate with the context.</param>
        /// <exception cref="ArgumentNullException">Thrown when helper or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CreateScope fails.</exception>
        public static void ExecuteInTenantContext(this TenantContextHelper helper, string tenantId, Action action, string userId = null)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

            using var scope = helper.CreateScope(tenantId, userId);
            action();
        }

        /// <summary>
        /// Executes a function within a tenant context scope, automatically restoring the previous context.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="tenantId">The tenant ID to set for the duration of the function.</param>
        /// <param name="func">The function to execute.</param>
        /// <param name="userId">Optional user ID to associate with the context.</param>
        /// <returns>The result of the function execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper or func is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CreateScope fails.</exception>
        public static T ExecuteInTenantContext<T>(this TenantContextHelper helper, string tenantId, Func<T> func, string userId = null)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(func);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

            using var scope = helper.CreateScope(tenantId, userId);
            return func();
        }

        /// <summary>
        /// Gets the current tenant context or throws if not available.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <returns>The current tenant context.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no tenant context is set.</exception>
        public static TenantContext GetRequiredTenantContext(this TenantContextHelper helper)
        {
            ArgumentNullException.ThrowIfNull(helper);

            var context = helper.GetTenantContext();
            if (context is null)
            {
                throw new InvalidOperationException("Tenant context is not set. Call SetTenantContext first.");
            }

            return context;
        }

        /// <summary>
        /// Checks if the current tenant context matches the expected tenant ID.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="expectedTenantId">The tenant ID to compare against.</param>
        /// <returns>True if the current tenant matches the expected tenant; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper or expectedTenantId is null.</exception>
        public static bool IsCurrentTenant(this TenantContextHelper helper, string expectedTenantId)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedTenantId, nameof(expectedTenantId));

            return string.Equals(helper.GetCurrentTenantId(), expectedTenantId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Executes an async action within a tenant context scope, automatically restoring the previous context.
        /// </summary>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="tenantId">The tenant ID to set for the duration of the action.</param>
        /// <param name="action">The async action to execute.</param>
        /// <param name="userId">Optional user ID to associate with the context.</param>
        /// <returns>A Task representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper or action is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CreateScope fails.</exception>
        public static async Task ExecuteInTenantContextAsync(this TenantContextHelper helper, string tenantId, Func<Task> action, string userId = null)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(action);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

            using var scope = helper.CreateScope(tenantId, userId);
            await action();
        }

        /// <summary>
        /// Executes an async function within a tenant context scope, automatically restoring the previous context.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="helper">The tenant context helper instance.</param>
        /// <param name="tenantId">The tenant ID to set for the duration of the function.</param>
        /// <param name="func">The async function to execute.</param>
        /// <param name="userId">Optional user ID to associate with the context.</param>
        /// <returns>The result of the async function execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when helper or func is null.</exception>
        /// <exception cref="ArgumentException">Thrown when tenantId is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CreateScope fails.</exception>
        public static async Task<T> ExecuteInTenantContextAsync<T>(this TenantContextHelper helper, string tenantId, Func<Task<T>> func, string userId = null)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(func);
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

            using var scope = helper.CreateScope(tenantId, userId);
            return await func();
        }
    }
}