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
    /// <summary>
    /// Processes items concurrently and collects the results and errors.
    /// </summary>
    /// <typeparam name="TItem">The type of item to process.</typeparam>
    /// <typeparam name="TResult">The type of result produced for each item.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="operation">The asynchronous operation to perform on each item.</param>
    /// <param name="maxConcurrency">The maximum number of operations to run concurrently.</param>
    /// <returns>A task containing the results and errors from the batch.</returns>
    Task<BatchProcessResult<TResult>> ProcessAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> operation,
        int maxConcurrency = 4);

    /// <summary>
    /// Processes items concurrently and collects any errors.
    /// </summary>
    /// <typeparam name="TItem">The type of item to process.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="operation">The asynchronous operation to perform on each item.</param>
    /// <param name="maxConcurrency">The maximum number of operations to run concurrently.</param>
    /// <returns>A task containing the outcome of the batch.</returns>
    Task<BatchProcessResult<object>> ProcessAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Task> operation,
        int maxConcurrency = 4);
}

/// <summary>
/// Processes batches concurrently while isolating errors for individual items.
/// </summary>
public sealed class BatchProcessor : IBatchProcessor {
    private readonly ILogger<BatchProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record batch processing activity.</param>
    public BatchProcessor(ILogger<BatchProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes items in a batch with result transformation.
    /// </summary>
    /// <typeparam name="TItem">The type of item to process.</typeparam>
    /// <typeparam name="TResult">The type of result produced for each item.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="operation">The asynchronous operation to perform on each item.</param>
    /// <param name="maxConcurrency">The maximum number of operations to run concurrently.</param>
    /// <returns>A task containing the results and errors from the batch.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    public async Task<BatchProcessResult<TResult>> ProcessAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> operation,
        int maxConcurrency = 4)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }
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
    /// <typeparam name="TItem">The type of item to process.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="operation">The asynchronous operation to perform on each item.</param>
    /// <param name="maxConcurrency">The maximum number of operations to run concurrently.</param>
    /// <returns>A task containing the outcome of the batch.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    public async Task<BatchProcessResult<object>> ProcessAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Task> operation,
        int maxConcurrency = 4)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }
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

/// <summary>
/// Contains the successful results and errors produced by a batch operation.
/// </summary>
/// <typeparam name="T">The type of each successful result.</typeparam>
public sealed class BatchProcessResult<T> {
    /// <summary>
    /// Gets or sets the successful results.
    /// </summary>
    public List<T> SuccessfulResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the errors encountered while processing items.
    /// </summary>
    public List<BatchErrorItem> Errors { get; set; } = new();

    /// <summary>
    /// Gets the number of successfully processed items.
    /// </summary>
    public int SuccessCount => SuccessfulResults.Count;

    /// <summary>
    /// Gets the number of items that failed processing.
    /// </summary>
    public int ErrorCount => Errors.Count;

    /// <summary>
    /// Gets the total number of processed items.
    /// </summary>
    public int TotalCount => SuccessCount + ErrorCount;

    /// <summary>
    /// Gets the proportion of processed items that succeeded.
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;

    /// <summary>
    /// Adds a successful result to the batch outcome.
    /// </summary>
    /// <param name="itemId">The identifier of the processed item.</param>
    /// <param name="result">The result produced for the item.</param>
    public void AddSuccess(string itemId, T result)
    {
        SuccessfulResults.Add(result);
    }

    /// <summary>
    /// Adds an error to the batch outcome.
    /// </summary>
    /// <param name="itemId">The identifier of the item that failed.</param>
    /// <param name="exception">The exception raised while processing the item.</param>
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

    /// <summary>
    /// Returns a summary of the batch outcome.
    /// </summary>
    /// <returns>A string containing the success count, error count, and success rate.</returns>
    public override string ToString()
    {
        return $"BatchProcessResult: {SuccessCount} success, {ErrorCount} errors, " +
               $"Success Rate: {SuccessRate:P2}";
    }
}

/// <summary>
/// Describes an error encountered while processing a batch item.
/// </summary>
public sealed class BatchErrorItem {
    /// <summary>
    /// Gets or sets the identifier of the item that failed.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type name of the exception.
    /// </summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception stack trace, if available.
    /// </summary>
    public string? StackTrace { get; set; }
}
