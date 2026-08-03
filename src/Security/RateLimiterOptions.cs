#nullable enable
using System;

namespace SqliteMultiTenant.Security;

/// <summary>
/// Options for configuring the <see cref="RateLimiter"/> behavior.
/// </summary>
public sealed class RateLimiterOptions
{
    /// <summary>
    /// Interval at which the cleanup timer runs to purge expired buckets.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time after which a bucket that has not been accessed is considered expired and removed.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromHours(1);
}
