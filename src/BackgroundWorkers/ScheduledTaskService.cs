#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Manages scheduled background tasks with configurable intervals.
/// Supports task registration, execution, and error handling.
/// Provides task status tracking and execution history.
/// </summary>
public interface IScheduledTaskService
{
    void RegisterTask(string taskId, Func<Task> taskAction, TimeSpan interval, string? tenantId = null);
    void UnregisterTask(string taskId);
    Task StartAsync();
    Task StopAsync();
    Task<TaskExecutionStatus> GetTaskStatusAsync(string taskId);
}

public sealed class ScheduledTaskService : IScheduledTaskService, IHostedService
{
    private readonly Dictionary<string, ScheduledTask> _tasks;
    private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens;
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly TenantContextHelper _tenantContextHelper;
    private bool _isRunning;

    public ScheduledTaskService(
        ILogger<ScheduledTaskService> logger,
        TenantContextHelper tenantContextHelper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantContextHelper = tenantContextHelper ?? throw new ArgumentNullException(nameof(tenantContextHelper));
        _tasks = new Dictionary<string, ScheduledTask>();
        _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
        _semaphore = new SemaphoreSlim(1);
        _isRunning = false;
    }

    /// <summary>
    /// Registers a new scheduled task.
    /// </summary>
    /// <param name="taskId">The unique identifier for the task.</param>
    /// <param name="taskAction">The action to execute.</param>
    /// <param name="interval">The interval between task executions.</param>
    /// <param name="tenantId">Optional tenant ID to associate with the task. If provided, the task will execute within that tenant's context.</param>
    /// <exception cref="ArgumentNullException">Thrown when taskAction is null.</exception>
    /// <exception cref="ArgumentException">Thrown when taskId is null or whitespace.</exception>
    public void RegisterTask(string taskId, Func<Task> taskAction, TimeSpan interval, string? tenantId = null)
    {
        ArgumentNullException.ThrowIfNull(taskAction);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId, nameof(taskId));

        try
        {
            _semaphore.Wait();

            var task = new ScheduledTask
            {
                Id = taskId,
                Action = taskAction,
                Interval = interval,
                LastExecutedAt = null,
                NextExecutionAt = DateTime.UtcNow.Add(interval),
                ExecutionCount = 0,
                FailureCount = 0,
                IsEnabled = true,
                IsExecuting = false,
                LastAttemptedAt = null,
                TenantId = tenantId
            };

            _tasks[taskId] = task;
            _logger.LogInformation("Scheduled task registered: {TaskId}, Interval: {TotalSeconds}s, TenantId: {TenantId}",
                taskId, interval.TotalSeconds, tenantId ?? "(none)");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Unregisters a scheduled task.
    /// </summary>
    /// <param name="taskId">The ID of the task to unregister.</param>
    public void UnregisterTask(string taskId)
    {
        try
        {
            _semaphore.Wait();

            if (_tasks.Remove(taskId))
            {
                if (_cancellationTokens.TryGetValue(taskId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _cancellationTokens.Remove(taskId);
                }

                _logger.LogInformation("Scheduled task unregistered: {TaskId}", taskId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Starts executing all registered tasks.
    /// </summary>
    public Task StartAsync() => StartAsync(CancellationToken.None);

    /// <summary>
    /// Stops all scheduled tasks.
    /// </summary>
    public Task StopAsync() => StopAsync(CancellationToken.None);

    /// <summary>
    /// Gets the status of a scheduled task.
    /// </summary>
    /// <param name="taskId">The ID of the task to query.</param>
    /// <returns>A <see cref="TaskExecutionStatus"/> object containing the task's status.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the task ID is not found.</exception>
    public async Task<TaskExecutionStatus> GetTaskStatusAsync(string taskId)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_tasks.TryGetValue(taskId, out var task))
            {
                var status = new TaskExecutionStatus
                {
                    TaskId = taskId,
                    IsEnabled = task.IsEnabled,
                    LastExecutedAt = task.LastExecutedAt,
                    NextExecutionAt = task.NextExecutionAt,
                    ExecutionCount = task.ExecutionCount,
                    FailureCount = task.FailureCount,
                    TenantId = task.TenantId
                };

                if (task.LastError != null)
                {
                    status.MarkFailed(task.LastError, task.LastExecutedAt, task.ExecutionCount, task.FailureCount);
                }
                else if (task.IsExecuting || _isRunning)
                {
                    status.MarkRunning();
                }
                else
                {
                    status.MarkCompleted(task.LastExecutedAt, task.ExecutionCount, task.FailureCount);
                }

                return status;
            }

            throw new KeyNotFoundException($"Task {taskId} not found");
        }
        finally
        {
            _semaphore.Release();
            await Task.CompletedTask;
        }
    }

    // IHostedService implementation
    Task IHostedService.StartAsync(CancellationToken cancellationToken) => StartAsync(cancellationToken);
    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);

    /// <summary>
    /// Starts executing all registered tasks with a cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_isRunning)
            {
                _logger.LogWarning("Task service is already running");
                return;
            }

            _isRunning = true;
            _logger.LogInformation("Scheduled task service started with {Count} tasks", _tasks.Count);

            // Start execution for each task
            foreach (var taskId in _tasks.Keys.ToList())
            {
                var cts = new CancellationTokenSource();
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                _cancellationTokens[taskId] = linkedCts;

                _ = ExecuteTaskLoopAsync(taskId, linkedCts.Token);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops all scheduled tasks with a cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (!_isRunning)
                return;

            // Cancel all running tasks
            foreach (var cts in _cancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _cancellationTokens.Clear();
            _isRunning = false;
            _logger.LogInformation("Scheduled task service stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ExecuteTaskLoopAsync(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ScheduledTask? task = null;
                await _semaphore.WaitAsync();
                try
                {
                    if (!_tasks.TryGetValue(taskId, out task))
                    {
                        break;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                if (task is null)
                    break;

                var now = DateTime.UtcNow;

                // Overlap prevention: Skip execution if previous run is still executing
                if (now >= task.NextExecutionAt && task.IsEnabled && !task.IsExecuting)
                {
                    await ExecuteTaskAsync(task);
                }

                // Check every second
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, exit gracefully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in task loop for {TaskId}", taskId);
        }
    }

    private async Task ExecuteTaskAsync(ScheduledTask task)
    {
        if (task is null)
        {
            _logger.LogError("Task is null");
            return;
        }

        try
        {
            // Mark task as executing
            task.IsExecuting = true;
            task.LastAttemptedAt = DateTime.UtcNow;

            _logger.LogDebug("Executing scheduled task: {Id}", task.Id);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Execute the task within the tenant context if TenantId is set
            if (!string.IsNullOrWhiteSpace(task.TenantId))
            {
                _tenantContextHelper.ExecuteInTenantContext(task.TenantId, async () =>
                {
                    await task.Action();
                });
            }
            else
            {
                // Execute without tenant context for backward compatibility
                await task.Action();
            }

            stopwatch.Stop();

            task.LastExecutedAt = DateTime.UtcNow;
            task.NextExecutionAt = DateTime.UtcNow.Add(GetNextInterval(task));
            task.ExecutionCount++;
            task.LastError = null;

            _logger.LogInformation(
                $"Task executed: {task.Id}, Duration: {stopwatch.ElapsedMilliseconds}ms, TenantId: {task.TenantId ?? "(none)"}");
        }
        catch (Exception ex)
        {
            task.FailureCount++;
            task.LastError = ex.Message;
            task.NextExecutionAt = DateTime.UtcNow.Add(GetNextInterval(task));

            _logger.LogError(ex, "Task failed: {Id}, Error: {Message}, TenantId: {TenantId}",
                task.Id, ex.Message, task.TenantId ?? "(none)");
        }
        finally
        {
            // Always mark task as not executing
            task.IsExecuting = false;
        }
    }

    /// <summary>
    /// Calculates the next execution interval with exponential backoff for consecutive failures.
    /// </summary>
    /// <param name="task">The task to calculate the interval for.</param>
    /// <returns>The calculated interval.</returns>
    private TimeSpan GetNextInterval(ScheduledTask task)
    {
        // Base interval
        var baseInterval = task.Interval;

        // Apply exponential backoff for failures
        // Formula: baseInterval * (2 ^ (failureCount - 1))
        // But cap at reasonable maximum to avoid excessive delays
        if (task.FailureCount > 0)
        {
            var backoffFactor = Math.Pow(2, Math.Min(task.FailureCount - 1, 5)); // Cap at 2^5 = 32x
            var backoffInterval = baseInterval.TotalSeconds * backoffFactor;
            var maxBackoff = TimeSpan.FromHours(24); // Maximum 24 hours backoff

            return TimeSpan.FromSeconds(Math.Min(backoffInterval, maxBackoff.TotalSeconds));
        }

        return baseInterval;
    }
}

public sealed class ScheduledTask
{
    public string Id { get; set; } = string.Empty;
    public Func<Task> Action { get; set; } = null!;
    public TimeSpan Interval { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextExecutionAt { get; set; }
    public long ExecutionCount { get; set; }
    public long FailureCount { get; set; }
    public bool IsEnabled { get; set; }
    public string? LastError { get; set; }
    public bool IsExecuting { get; set; }
    public DateTime? LastAttemptedAt { get; set; }
    public string? TenantId { get; set; }
}

public sealed class TaskExecutionStatus : OperationStatusBase
{
    /// <summary>
    /// The unique identifier for the task.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the task is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The timestamp when the task was last executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// The timestamp when the task will next execute.
    /// </summary>
    public DateTime NextExecutionAt { get; set; }

    /// <summary>
    /// The total number of times the task has executed.
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// The total number of times the task has failed.
    /// </summary>
    public long FailureCount { get; set; }

    /// <summary>
    /// The tenant ID associated with this task, if any.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskExecutionStatus"/> class.
    /// </summary>
    public TaskExecutionStatus()
    {
        OperationId = nameof(TaskExecutionStatus);
        Status = OperationStatus.Running;
        StartedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the status to completed successfully.
    /// </summary>
    /// <param name="lastExecutedAt">The timestamp when the task was last executed.</param>
    /// <param name="executionCount">The total execution count.</param>
    /// <param name="failureCount">The total failure count.</param>
    public void MarkCompleted(DateTime? lastExecutedAt, long executionCount, long failureCount)
    {
        MarkCompleted();
        LastExecutedAt = lastExecutedAt;
        ExecutionCount = executionCount;
        FailureCount = failureCount;
    }

    /// <summary>
    /// Updates the status to failed.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="lastExecutedAt">The timestamp when the task was last executed.</param>
    /// <param name="executionCount">The total execution count.</param>
    /// <param name="failureCount">The total failure count.</param>
    public void MarkFailed(string error, DateTime? lastExecutedAt, long executionCount, long failureCount)
    {
        MarkFailed(error);
        LastExecutedAt = lastExecutedAt;
        ExecutionCount = executionCount;
        FailureCount = failureCount;
    }

    /// <summary>
    /// Updates the status to running.
    /// </summary>
    public void MarkRunning()
    {
        MarkRunning();
    }

    /// <summary>
    /// Validates the task execution status.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the status is invalid.</exception>
    public void Validate()
    {
        ValidateStatus();
    }
}