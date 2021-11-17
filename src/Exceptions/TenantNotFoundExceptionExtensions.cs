using System;

namespace SqliteMultiTenant.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="TenantNotFoundException"/> to facilitate error handling and tenant identification.
    /// </summary>
    public static class TenantNotFoundExceptionExtensions
    {
        /// <summary>
        /// Determines whether the exception's tenant ID matches the specified tenant ID.
        /// </summary>
        /// <param name="exception">The exception instance. Cannot be <see langword="null"/>.</param>
        /// <param name="tenantId">The tenant ID to compare against. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the tenant IDs match; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static bool IsMatchingTenantId(this TenantNotFoundException exception, string? tenantId)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.TenantId == tenantId;
        }

        /// <summary>
        /// Generates a formatted error message for the tenant not found exception.
        /// </summary>
        /// <param name="exception">The exception instance. Cannot be <see langword="null"/>.</param>
        /// <returns>A formatted error message containing the tenant ID.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static string GetErrorMessage(this TenantNotFoundException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return $"Tenant with ID {exception.TenantId} not found.";
        }

        /// <summary>
        /// Wraps this exception as an inner exception in an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="exception">The exception instance. Cannot be <see langword="null"/>.</param>
        /// <returns>A new <see cref="InvalidOperationException"/> with this exception as the inner exception.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static Exception AsInnerException(this TenantNotFoundException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new InvalidOperationException("Tenant not found", exception);
        }
    }
}