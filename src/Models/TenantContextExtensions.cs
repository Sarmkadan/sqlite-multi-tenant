using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SqliteMultiTenant.Models
{
    /// <summary>
    /// Extension methods that add useful functionality to <see cref="TenantContext"/>.
    /// </summary>
    public static class TenantContextExtensions
    {
        /// <summary>
        /// Retrieves a value from <see cref="TenantContext.ContextData"/> and attempts to cast it to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the stored value.</typeparam>
        /// <param name="context">The tenant context.</param>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value cast to <typeparamref name="T"/>, or <c>default</c> if the key does not exist or the cast fails.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is <c>null</c> or empty.</exception>
        public static T? GetTypedContextData<T>(this TenantContext context, string key)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (context.ContextData is null)
                return default;

            return context.ContextData.TryGetValue(key, out var value) && value is T typed
                ? typed
                : default;
        }

        /// <summary>
        /// Sets a value in <see cref="TenantContext.ContextData"/> only if the key is not already present.
        /// </summary>
        /// <param name="context">The tenant context.</param>
        /// <param name="key">The key under which to store the value.</param>
        /// <param name="value">The value to store.</param>
        /// <returns><c>true</c> if the value was added; <c>false</c> if the key already existed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is <c>null</c> or empty.</exception>
        public static bool SetContextDataIfAbsent(this TenantContext context, string key, object value)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(value);

            // Ensure the dictionary exists.
            if (context.ContextData is null)
            {
                // Use the public SetContextData method to initialise the dictionary.
                context.SetContextData(key, value);
                return true;
            }

            if (context.ContextData.ContainsKey(key))
                return false;

            context.SetContextData(key, value);
            return true;
        }

        /// <summary>
        /// Determines whether the tenant context represents an active tenant.
        /// </summary>
        /// <param name="context">The tenant context.</param>
        /// <returns><c>true</c> if the context is valid and has a non‑empty <see cref="TenantContext.TenantId"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        public static bool IsActive(this TenantContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return context.IsValid && !string.IsNullOrWhiteSpace(context.TenantId);
        }

        /// <summary>
        /// Returns a concise, machine‑friendly summary of the tenant context.
        /// </summary>
        /// <param name="context">The tenant context.</param>
        /// <returns>A string containing the tenant identifier, name and user email.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        public static string ToSummaryString(this TenantContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return string.Create(CultureInfo.InvariantCulture, $"{context.TenantId}|{context.TenantName ?? "N/A"}|{context.UserEmail ?? "N/A"}");
        }
    }
}
