#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Globalization;
using SqliteMultiTenant.Exceptions;

namespace SqliteMultiTenant.Tenants;

/// <summary>
/// Helper methods for <see cref="QuotaCheckResult"/>.
/// </summary>
public static class QuotaCheckResultExtensions
{
    /// <summary>
    /// Throws a <see cref="QuotaExceededException"/> when the quota check reports the tenant as over quota.
    /// Does nothing when the tenant is within quota or has no quota configured (unlimited).
    /// </summary>
    /// <param name="result">The quota check result to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <exception cref="QuotaExceededException">Thrown when <see cref="QuotaCheckResult.IsOverQuota"/> is true and a quota is configured.</exception>
    /// <remarks>
    /// The check result does not carry the size delta of a pending operation, so the exception's
    /// <see cref="QuotaExceededException.DeltaBytes"/> is set to 0 and the proposed size equals the current size.
    /// </remarks>
    public static void ThrowIfExceeded(this QuotaCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsOverQuota || result.QuotaBytes is not long quotaBytes)
        {
            return;
        }

        throw new QuotaExceededException(
            result.TenantId,
            quotaBytes,
            result.CurrentSizeBytes,
            deltaBytes: 0);
    }

    /// <summary>
    /// Calculates the current usage as a percentage of the configured quota.
    /// </summary>
    /// <param name="result">The quota check result to measure.</param>
    /// <returns>
    /// Usage in the range 0 and above (100 means exactly at quota). Returns 0 when no quota is configured
    /// or the configured quota is zero or negative, so the value is always finite.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static double CalculateUsagePercent(this QuotaCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.QuotaBytes is not long quotaBytes || quotaBytes <= 0)
        {
            return 0;
        }

        return (double)result.CurrentSizeBytes / quotaBytes * 100;
    }

    /// <summary>
    /// Formats the quota check result as a short human-readable line for logs and CLI output.
    /// </summary>
    /// <param name="result">The quota check result to format.</param>
    /// <returns>
    /// A string such as <c>"tenant-1: 1.5 MB / 10 MB (15.00%)"</c>, with a <c>" [OVER QUOTA]"</c> or
    /// <c>" [NEAR QUOTA]"</c> suffix when applicable. Tenants without a quota render as
    /// <c>"tenant-1: 1.5 MB (no quota)"</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static string ToDisplayString(this QuotaCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var current = FormatSize(result.CurrentSizeBytes);

        if (result.QuotaBytes is not long quotaBytes)
        {
            return FormattableString.Invariant($"{result.TenantId}: {current} (no quota)");
        }

        var quota = FormatSize(quotaBytes);
        var percent = result.CalculateUsagePercent();
        var suffix = result.IsOverQuota
            ? " [OVER QUOTA]"
            : result.IsNearQuota
                ? " [NEAR QUOTA]"
                : string.Empty;

        return FormattableString.Invariant($"{result.TenantId}: {current} / {quota} ({percent:F2}%){suffix}");
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double len = bytes;
        while (Math.Abs(len) >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return len.ToString("0.##", CultureInfo.InvariantCulture) + " " + sizes[order];
    }
}
