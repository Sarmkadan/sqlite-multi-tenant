#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =========================================================================

using System;
using System.Text.Json.Serialization;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the lifecycle status of an operation.
/// </summary>
[JsonConverter(typeof(OperationStatusJsonConverter))]
public enum OperationStatus
{
    /// <summary>
    /// The operation is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// The operation is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The operation failed.
    /// </summary>
    Failed
}

/// <summary>
/// Common operation status information shared by all operation status types.
/// </summary>
public abstract class OperationStatusBase
{
    /// <summary>
    /// The unique identifier for the operation.
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the operation.
    /// </summary>
    public OperationStatus Status { get; set; }

    /// <summary>
    /// The timestamp when the operation started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// The timestamp when the operation completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if the operation failed, otherwise null.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The timestamp when the operation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Indicates whether the operation is currently running.
    /// </summary>
    [JsonIgnore]
    public bool IsRunning => Status == OperationStatus.Running;

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    [JsonIgnore]
    public bool IsCompleted => Status == OperationStatus.Succeeded;

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => Status == OperationStatus.Failed;

    /// <summary>
    /// Indicates whether the operation is pending execution.
    /// </summary>
    [JsonIgnore]
    public bool IsPending => Status == OperationStatus.Pending;

    /// <summary>
    /// The duration of the operation in milliseconds (if completed).
    /// </summary>
    [JsonIgnore]
    public long? DurationMs
    {
        get
        {
            if (StartedAt == null || CompletedAt == null)
                return null;

            return (long)(CompletedAt.Value - StartedAt.Value).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Gets the string representation of the current status.
    /// </summary>
    [JsonIgnore]
    public string State => Status.ToString().ToLowerInvariant();

    /// <summary>
    /// Validates the operation status.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the status is invalid.</exception>
    protected void ValidateStatus()
    {
        if (StartedAt == null && Status != OperationStatus.Pending)
        {
            throw new ArgumentException(
                "StartedAt must be set when status is not Pending",
                nameof(StartedAt));
        }

        if (Status == OperationStatus.Succeeded && CompletedAt == null)
        {
            throw new ArgumentException(
                "CompletedAt must be set when status is Succeeded",
                nameof(CompletedAt));
        }

        if (Status == OperationStatus.Failed && CompletedAt == null && string.IsNullOrEmpty(Error))
        {
            throw new ArgumentException(
                "CompletedAt and Error must be set when status is Failed",
                nameof(CompletedAt));
        }
    }

    /// <summary>
    /// Marks the operation as running.
    /// </summary>
    protected void MarkRunning()
    {
        Status = OperationStatus.Running;
        StartedAt ??= DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the operation as completed successfully.
    /// </summary>
    protected void MarkCompleted()
    {
        Status = OperationStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
        Error = null;
    }

    /// <summary>
    /// Marks the operation as failed.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    protected void MarkFailed(string error)
    {
        Status = OperationStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Error = error;
    }
}