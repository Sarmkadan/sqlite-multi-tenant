using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqliteMultiTenant.Api;          // ApiResponse<T>
using SqliteMultiTenant.Models;      // TenantResponse (assumed location)

namespace SqliteMultiTenant.Api.Controllers
{
    /// <summary>
    /// Extension methods for <see cref="TenantController"/>.
    /// </summary>
    public static class TenantControllerExtensions
    {
        /// <summary>
        /// Returns the simple name of the controller type.
        /// </summary>
        public static string GetControllerName(this TenantController controller) =>
            controller.GetType().Name;

        /// <summary>
        /// Executes an asynchronous operation with a simple retry policy.
        /// The operation is invoked up to <paramref name="maxAttempts"/> times
        /// until a successful <see cref="ApiResponse{T}"/> is returned.
        /// </summary>
        public static async Task<ApiResponse<T>> ExecuteWithRetryAsync<T>(
            this TenantController controller,
            Func<Task<ApiResponse<T>>> operation,
            int maxAttempts = 3)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            ApiResponse<T>? lastResult = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                lastResult = await operation();

                if (lastResult != null && lastResult.IsSuccess)
                {
                    return lastResult;
                }
            }

            // Return the last attempt (may be a failure) so the caller can handle it.
            return lastResult!;
        }

        /// <summary>
        /// Retrieves all tenants and returns them as a dictionary keyed by tenant identifier.
        /// The method uses reflection to obtain the identifier property named <c>TenantId</c>
        /// from each <see cref="TenantResponse"/> instance, avoiding a hard compile‑time
        /// dependency on the exact shape of the type.
        /// </summary>
        public static async Task<Dictionary<string, TenantResponse>> GetAllTenantsAsDictionaryAsync(
            this TenantController controller)
        {
            var response = await controller.ListAllTenantsAsync();

            var dict = new Dictionary<string, TenantResponse>(StringComparer.OrdinalIgnoreCase);

            if (response.IsSuccess && response.Data != null)
            {
                foreach (var tenant in response.Data)
                {
                    // Expect a property named "TenantId" on TenantResponse.
                    var idProp = tenant.GetType().GetProperty("TenantId");
                    if (idProp != null)
                    {
                        var id = idProp.GetValue(tenant) as string;
                        if (!string.IsNullOrEmpty(id))
                        {
                            dict[id] = tenant;
                        }
                    }
                }
            }

            return dict;
        }
    }
}
