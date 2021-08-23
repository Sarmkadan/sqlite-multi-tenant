#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Operations;

/// <summary>
/// Processes batch operations with error isolation and progress tracking.
/// Supports concurrent batch processing with configurable concurrency levels.
/// Provides detailed results and error reporting for failed items.
/// </summary>
public interface IBatchProcessor
{
    Task<BatchProcessResult<TResult>> ProcessAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> operation,
        int maxConcurrency = 4);

    Task<BatchProcessResult<object>> ProcessAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Task> operation,
        int maxConcurrency = 4);
}

public sealed class BatchProcessor : IBatchProcessor {
    private readonly ILogger<BatchProcessor> _logger;

    public BatchProcessor(ILogger<BatchProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes items in a batch with result transformation.
    /// </summary>
    public async Task<BatchProcessResult<TResult>> ProcessAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> operation,
        int maxConcurrency = 4)
    {
        var result = new BatchProcessResult<TResult>();
        var itemList = items.ToList();

        _logger.LogInformation("Starting batch processing: {Count} items, Concurrency: {MaxConcurrency}", itemList.Count, maxConcurrency);

        using (var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency))
        {
            var tasks = itemList.Select(async (item, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug($"Processing item {index + 1}/{itemList.Count}");
                    var output = await operation(item);
                    result.AddSuccess(index.ToString(), output);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing item {Index}: {Message}", index, ex.Message);
                    result.AddError(index.ToString(), ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        _logger.LogInformation(
            $"Batch processing completed: {result.SuccessCount} succeeded, " +
            $"{result.ErrorCount} failed");

        return result;
    }

    /// <summary>
    /// Processes items without result transformation.
    /// </summary>
    public async Task<BatchProcessResult<object>> ProcessAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Task> operation,
        int maxConcurrency = 4)
    {
        var result = new BatchProcessResult<object>();
        var itemList = items.ToList();

        _logger.LogInformation("Starting batch processing: {Count} items, Concurrency: {MaxConcurrency}", itemList.Count, maxConcurrency);

        using (var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency))
        {
            var tasks = itemList.Select(async (item, index) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    _logger.LogDebug($"Processing item {index + 1}/{itemList.Count}");
                    await operation(item);
                    result.AddSuccess(index.ToString(), new object());
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing item {Index}: {Message}", index, ex.Message);
                    result.AddError(index.ToString(), ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        _logger.LogInformation(
            $"Batch processing completed: {result.SuccessCount} succeeded, " +
            $"{result.ErrorCount} failed");

        return result;
    }
}

public sealed class BatchProcessResult<T> {
    public List<T> SuccessfulResults { get; set; } = new();
    public List<BatchErrorItem> Errors { get; set; } = new();
    public int SuccessCount => SuccessfulResults.Count;
    public int ErrorCount => Errors.Count;
    public int TotalCount => SuccessCount + ErrorCount;
    public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;

    public void AddSuccess(string itemId, T result)
    {
        SuccessfulResults.Add(result);
    }

    public void AddError(string itemId, Exception exception)
    {
        Errors.Add(new BatchErrorItem
        {
            ItemId = itemId,
            Exception = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace
        });
    }

    public override string ToString()
    {
        return $"BatchProcessResult: {SuccessCount} success, {ErrorCount} errors, " +
               $"Success Rate: {SuccessRate:P2}";
    }
}

public sealed class BatchErrorItem {
    public string ItemId { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
