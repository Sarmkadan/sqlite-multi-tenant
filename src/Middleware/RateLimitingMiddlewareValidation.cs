#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;

namespace SqliteMultiTenant.Middleware;

/// <summary>
/// Validation helpers for <see cref="RateLimitingMiddleware"/> and related types.
/// Provides comprehensive validation for configuration and runtime values.
/// </summary>
public static class RateLimitingMiddlewareValidation
{
  /// <summary>
  /// Validates a <see cref="RateLimitingMiddleware"/> instance.
  /// </summary>
  /// <param name="value">The middleware instance to validate.</param>
  /// <returns>List of human-readable validation problems; empty list if valid.</returns>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
  public static IReadOnlyList<string> Validate(this RateLimitingMiddleware value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var problems = new List<string>();

    // RateLimitingOptions validation
    problems.AddRange(value._options.Validate());

    return problems.AsReadOnly();
  }

  /// <summary>
  /// Validates a <see cref="RateLimitingOptions"/> instance.
  /// </summary>
  /// <param name="options">The options to validate.</param>
  /// <returns>List of human-readable validation problems; empty list if valid.</returns>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
  public static IReadOnlyList<string> Validate(this RateLimitingOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var problems = new List<string>();

    if (options.RequestsPerMinute <= 0)
    {
      problems.Add($"{nameof(RateLimitingOptions.RequestsPerMinute)} must be positive, but was {options.RequestsPerMinute}.");
    }

    if (options.BurstCapacity < 0)
    {
      problems.Add($"{nameof(RateLimitingOptions.BurstCapacity)} must be non-negative, but was {options.BurstCapacity}.");
    }

    if (options.CleanupIntervalSeconds <= 0)
    {
      problems.Add($"{nameof(RateLimitingOptions.CleanupIntervalSeconds)} must be positive, but was {options.CleanupIntervalSeconds}.");
    }

    return problems.AsReadOnly();
  }

  /// <summary>
  /// Validates a <see cref="TokenBucket"/> instance.
  /// </summary>
  /// <param name="bucket">The bucket to validate.</param>
  /// <returns>List of human-readable validation problems; empty list if valid.</returns>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="bucket"/> is null.</exception>
  public static IReadOnlyList<string> Validate(this TokenBucket bucket)
  {
    ArgumentNullException.ThrowIfNull(bucket);

    // TokenBucket is validated during construction and has no mutable public state
    // that can become invalid at runtime. All validation is done via constructor parameters.
    return Array.Empty<string>();
  }

  /// <summary>
  /// Determines whether a <see cref="RateLimitingMiddleware"/> instance is valid.
  /// </summary>
  /// <param name="value">The middleware instance to check.</param>
  /// <returns>True if valid; otherwise, false.</returns>
  public static bool IsValid(this RateLimitingMiddleware value) => value.Validate().Count == 0;

  /// <summary>
  /// Determines whether a <see cref="RateLimitingOptions"/> instance is valid.
  /// </summary>
  /// <param name="options">The options to check.</param>
  /// <returns>True if valid; otherwise, false.</returns>
  public static bool IsValid(this RateLimitingOptions options) => options.Validate().Count == 0;

  /// <summary>
  /// Determines whether a <see cref="TokenBucket"/> instance is valid.
  /// </summary>
  /// <param name="bucket">The bucket to check.</param>
  /// <returns>True if valid; otherwise, false.</returns>
  public static bool IsValid(this TokenBucket bucket) => bucket.Validate().Count == 0;

  /// <summary>
  /// Ensures that a <see cref="RateLimitingMiddleware"/> instance is valid.
  /// </summary>
  /// <param name="value">The middleware instance to validate.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
  public static void EnsureValid(this RateLimitingMiddleware value)
  {
    ArgumentNullException.ThrowIfNull(value);

    var problems = value.Validate();
    if (problems.Count > 0)
    {
      throw new ArgumentException($"RateLimitingMiddleware is not valid. Problems:\n{string.Join("\n", problems)}");
    }
  }

  /// <summary>
  /// Ensures that a <see cref="RateLimitingOptions"/> instance is valid.
  /// </summary>
  /// <param name="options">The options to validate.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown if <paramref name="options"/> is not valid, containing a list of problems.</exception>
  public static void EnsureValid(this RateLimitingOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var problems = options.Validate();
    if (problems.Count > 0)
    {
      throw new ArgumentException($"RateLimitingOptions is not valid. Problems:\n{string.Join("\n", problems)}");
    }
  }

  /// <summary>
  /// Ensures that a <see cref="TokenBucket"/> instance is valid.
  /// </summary>
  /// <param name="bucket">The bucket to validate.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="bucket"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown if <paramref name="bucket"/> is not valid, containing a list of problems.</exception>
  public static void EnsureValid(this TokenBucket bucket)
  {
    ArgumentNullException.ThrowIfNull(bucket);
  }
}