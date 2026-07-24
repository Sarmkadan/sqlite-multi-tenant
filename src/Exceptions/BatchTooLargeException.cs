#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =======================================================================

namespace SqliteMultiTenant.Exceptions;

/// <summary>
/// Thrown when a batch operation exceeds configured size limits to prevent resource exhaustion attacks.
/// </summary>
public sealed class BatchTooLargeException : Exception
{
    /// <summary>Gets the maximum allowed item count.</summary>
    public int MaxItemCount { get; }

    /// <summary>Gets the actual item count in the batch.</summary>
    public int ActualItemCount { get; }

    /// <summary>Gets the maximum allowed payload size in bytes.</summary>
    public long MaxPayloadSizeBytes { get; }

    /// <summary>Gets the actual payload size in bytes.</summary>
    public long ActualPayloadSizeBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTooLargeException"/> class.
    /// </summary>
    /// <param name="maxItemCount">The maximum allowed item count.</param>
    /// <param name="actualItemCount">The actual item count in the batch.</param>
    public BatchTooLargeException(int maxItemCount, int actualItemCount)
        : base(FormatMessage(maxItemCount, actualItemCount, 0, 0))
    {
        MaxItemCount = maxItemCount;
        ActualItemCount = actualItemCount;
        MaxPayloadSizeBytes = 0;
        ActualPayloadSizeBytes = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTooLargeException"/> class.
    /// </summary>
    /// <param name="maxItemCount">The maximum allowed item count.</param>
    /// <param name="actualItemCount">The actual item count in the batch.</param>
    /// <param name="maxPayloadSizeBytes">The maximum allowed payload size in bytes.</param>
    /// <param name="actualPayloadSizeBytes">The actual payload size in bytes.</param>
    public BatchTooLargeException(
        int maxItemCount,
        int actualItemCount,
        long maxPayloadSizeBytes,
        long actualPayloadSizeBytes)
        : base(FormatMessage(maxItemCount, actualItemCount, maxPayloadSizeBytes, actualPayloadSizeBytes))
    {
        MaxItemCount = maxItemCount;
        ActualItemCount = actualItemCount;
        MaxPayloadSizeBytes = maxPayloadSizeBytes;
        ActualPayloadSizeBytes = actualPayloadSizeBytes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTooLargeException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="maxItemCount">The maximum allowed item count.</param>
    /// <param name="actualItemCount">The actual item count in the batch.</param>
    public BatchTooLargeException(
        string message,
        int maxItemCount,
        int actualItemCount)
        : base(message)
    {
        MaxItemCount = maxItemCount;
        ActualItemCount = actualItemCount;
        MaxPayloadSizeBytes = 0;
        ActualPayloadSizeBytes = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTooLargeException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="maxItemCount">The maximum allowed item count.</param>
    /// <param name="actualItemCount">The actual item count in the batch.</param>
    /// <param name="innerException">The inner exception.</param>
    public BatchTooLargeException(
        string message,
        int maxItemCount,
        int actualItemCount,
        Exception? innerException)
        : base(message, innerException)
    {
        MaxItemCount = maxItemCount;
        ActualItemCount = actualItemCount;
        MaxPayloadSizeBytes = 0;
        ActualPayloadSizeBytes = 0;
    }

    private static string FormatMessage(
        int maxItemCount,
        int actualItemCount,
        long maxPayloadSizeBytes,
        long actualPayloadSizeBytes)
    {
        var message = $"Batch operation exceeds configured size limits. " +
                     $"Max items: {maxItemCount}, Actual items: {actualItemCount}.";

        if (maxPayloadSizeBytes > 0 && actualPayloadSizeBytes > 0)
        {
            message += $" " +
                     $"Max payload size: {FormatSize(maxPayloadSizeBytes)}, " +
                     $"Actual payload size: {FormatSize(actualPayloadSizeBytes)}.";
        }

        return message;
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
