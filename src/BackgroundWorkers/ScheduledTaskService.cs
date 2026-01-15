#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.BackgroundWorkers;

/// <summary>
/// Manages scheduled background tasks with configurable intervals.
/// Supports task registration, execution, and error handling.
/// Provides task status tracking and execution history.
/// </summary>
public interface IScheduledTaskService
{
    void RegisterTask(string taskId, Func<Task> taskAction, TimeSpan interval);
    void UnregisterTask(string taskId);
    Task StartAsync();
    Task StopAsync();
    Task<TaskExecutionStatus> GetTaskStatusAsync(string taskId);
}

public sealed class ScheduledTaskService : IScheduledTaskService {
    private readonly Dictionary<string, ScheduledTask> _tasks;
    private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens;
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly SemaphoreSlim _semaphore;
    private bool _isRunning;

    public ScheduledTaskService(ILogger<ScheduledTaskService> logger)
    {
        _logger = logger;
        _tasks = new Dictionary<string, ScheduledTask>();
        _cancellationTokens = new Dictionary<string, CancellationTokenSource>();
        _semaphore = new SemaphoreSlim(1);
        _isRunning = false;
    }

    /// <summary>
    /// Registers a new scheduled task.
    /// </summary>
    public void RegisterTask(string taskId, Func<Task> taskAction, TimeSpan interval)
    {
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
                IsEnabled = true
            };

            _tasks[taskId] = task;
            _logger.LogInformation($"Scheduled task registered: {taskId}, Interval: {interval.TotalSeconds}s");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Unregisters a scheduled task.
    /// </summary>
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

                _logger.LogInformation($"Scheduled task unregistered: {taskId}");
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
    public async Task StartAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_isRunning)
            {
                _logger.LogWarning("Task service is already running");
                return;
            }

            _isRunning = true;
            _logger.LogInformation($"Scheduled task service started with {_tasks.Count} tasks");

            // Start execution for each task
            foreach (var taskId in _tasks.Keys.ToList())
            {
                var cts = new CancellationTokenSource();
                _cancellationTokens[taskId] = cts;

                _ = ExecuteTaskLoopAsync(taskId, cts.Token);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops all scheduled tasks.
    /// </summary>
    public async Task StopAsync()
    {
        try
        {
            await _semaphore.WaitAsync();

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

    /// <summary>
    /// Gets the status of a scheduled task.
    /// </summary>
    public async Task<TaskExecutionStatus> GetTaskStatusAsync(string taskId)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (_tasks.TryGetValue(taskId, out var task))
            {
                return new TaskExecutionStatus
                {
                    TaskId = taskId,
                    IsEnabled = task.IsEnabled,
                    IsRunning = _isRunning,
                    LastExecutedAt = task.LastExecutedAt,
                    NextExecutionAt = task.NextExecutionAt,
                    ExecutionCount = task.ExecutionCount,
                    FailureCount = task.FailureCount,
                    LastError = task.LastError
                };
            }

            throw new KeyNotFoundException($"Task {taskId} not found");
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
                if (!_tasks.TryGetValue(taskId, out var task))
                    break;

                var now = DateTime.UtcNow;

                if (now >= task.NextExecutionAt && task.IsEnabled)
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
            _logger.LogError($"Error in task loop for {taskId}: {ex.Message}");
        }
    }

    private async Task ExecuteTaskAsync(ScheduledTask task)
    {
        try
        {
            _logger.LogDebug($"Executing scheduled task: {task.Id}");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await task.Action();

            stopwatch.Stop();

            task.LastExecutedAt = DateTime.UtcNow;
            task.NextExecutionAt = DateTime.UtcNow.Add(task.Interval);
            task.ExecutionCount++;
            task.LastError = null;

            _logger.LogInformation(
                $"Task executed: {task.Id}, Duration: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            task.FailureCount++;
            task.LastError = ex.Message;
            task.NextExecutionAt = DateTime.UtcNow.Add(task.Interval);

            _logger.LogError($"Task failed: {task.Id}, Error: {ex.Message}");
        }
    }
}

public sealed class ScheduledTask {
    public string Id { get; set; } = string.Empty;
    public Func<Task> Action { get; set; } = null!;
    public TimeSpan Interval { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextExecutionAt { get; set; }
    public long ExecutionCount { get; set; }
    public long FailureCount { get; set; }
    public bool IsEnabled { get; set; }
    public string? LastError { get; set; }
}

public sealed class TaskExecutionStatus {
    public string TaskId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextExecutionAt { get; set; }
    public long ExecutionCount { get; set; }
    public long FailureCount { get; set; }
    public string? LastError { get; set; }
}
