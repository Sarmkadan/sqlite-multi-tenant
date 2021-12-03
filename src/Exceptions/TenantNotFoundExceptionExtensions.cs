using System;

namespace SqliteMultiTenant.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="TenantNotFoundException"/> to facilitate error handling
    /// and tenant identification.
    /// </summary>
    public static class TenantNotFoundExceptionExtensions
    {
        /// <summary>
        /// Determines whether the exception's <see cref="TenantNotFoundException.TenantId"/> matches the
        /// specified <paramref name="tenantId"/>.
        /// </summary>
        /// <param name="exception">
        /// The <see cref="TenantNotFoundException"/> instance. This argument cannot be <c>null</c>.
        /// </param>
        /// <param name="tenantId">
        /// The tenant identifier to compare against. This value may be <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="exception"/>'s <c>TenantId</c> equals <paramref name="tenantId"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
        public static bool IsMatchingTenantId(this TenantNotFoundException exception, string? tenantId)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.TenantId == tenantId;
        }

        /// <summary>
        /// Generates a formatted error message that includes the tenant identifier from the exception.
        /// </summary>
        /// <param name="exception">
        /// The <see cref="TenantNotFoundException"/> instance. This argument cannot be <c>null</c>.
        /// </param>
        /// <returns>
        /// A string in the form <c>"Tenant with ID {TenantId} not found."</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
        public static string GetErrorMessage(this TenantNotFoundException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return $"Tenant with ID {exception.TenantId} not found.";
        }

        /// <summary>
        /// Wraps the supplied <see cref="TenantNotFoundException"/> in an <see cref="InvalidOperationException"/>
        /// as its inner exception.
        /// </summary>
        /// <param name="exception">
        /// The <see cref="TenantNotFoundException"/> instance to wrap. This argument cannot be <c>null</c>.
        /// </param>
        /// <returns>
        /// A new <see cref="InvalidOperationException"/> with the message <c>"Tenant not found"</c> and the
        /// original exception set as its <c>InnerException</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
        public static Exception AsInnerException(this TenantNotFoundException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new InvalidOperationException("Tenant not found", exception);
        }
    }
}
