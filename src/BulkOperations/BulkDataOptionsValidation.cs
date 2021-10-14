#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.BulkOperations;

/// <summary>
/// Provides validation helpers for <see cref="BulkDataOptions"/> configuration.
/// </summary>
public static class BulkDataOptionsValidation
{
    /// <summary>
    /// Validates the specified <see cref="BulkDataOptions"/> instance.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this BulkDataOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.DefaultBatchSize <= 0)
        {
            problems.Add($"{nameof(BulkDataOptions.DefaultBatchSize)} must be positive, but was {value.DefaultBatchSize}.");
        }

        if (value.MaxConcurrentTables <= 0)
        {
            problems.Add($"{nameof(BulkDataOptions.MaxConcurrentTables)} must be positive, but was {value.MaxConcurrentTables}.");
        }

        if (value.MaxBufferSizeBytes <= 0)
        {
            problems.Add($"{nameof(BulkDataOptions.MaxBufferSizeBytes)} must be positive, but was {value.MaxBufferSizeBytes}.");
        }

        if (value.OperationTimeout <= TimeSpan.Zero)
        {
            problems.Add($"{nameof(BulkDataOptions.OperationTimeout)} must be positive, but was {value.OperationTimeout}.");
        }

        if (string.IsNullOrWhiteSpace(value.DefaultExportDirectory))
        {
            problems.Add($"{nameof(BulkDataOptions.DefaultExportDirectory)} cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.BaseDatabasePath))
        {
            problems.Add($"{nameof(BulkDataOptions.BaseDatabasePath)} cannot be null or whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BulkDataOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this BulkDataOptions value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BulkDataOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid; the message lists all validation problems.</exception>
    public static void EnsureValid(this BulkDataOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"BulkDataOptions is invalid. Problems: {string.Join(" ", problems)}");
    }
}