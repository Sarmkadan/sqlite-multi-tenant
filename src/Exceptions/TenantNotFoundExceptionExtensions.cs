using System;

namespace SqliteMultiTenant.Exceptions
{
    public static class TenantNotFoundExceptionExtensions
    {
        public static bool IsMatchingTenantId(this TenantNotFoundException exception, string? tenantId)
        {
            return exception.TenantId == tenantId;
        }

        public static string GetErrorMessage(this TenantNotFoundException exception)
        {
            return $"Tenant with ID {exception.TenantId} not found.";
        }

        public static Exception AsInnerException(this TenantNotFoundException exception)
        {
            return new InvalidOperationException("Tenant not found", exception);
        }
    }
}
