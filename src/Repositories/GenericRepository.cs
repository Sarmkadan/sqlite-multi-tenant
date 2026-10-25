#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Repositories;

/// <summary>
/// Generic repository base class implementing common CRUD operations.
/// Provides template for data access with filtering, paging, and sorting.
/// Supports both sync and async operations for maximum flexibility.
/// </summary>
public abstract class GenericRepository<T> where T : class
{
    protected readonly ILogger _logger;

    protected GenericRepository(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    public abstract Task<List<T>> GetAllAsync();

    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    public abstract Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    public abstract Task<T> CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    public abstract Task<bool> UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity by ID.
    /// </summary>
    public abstract Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Gets entities matching a filter.
    /// </summary>
    public abstract Task<List<T>> FindAsync(Func<T, bool> predicate);

    /// <summary>
    /// Gets the count of all entities.
    /// </summary>
    public abstract Task<int> GetCountAsync();

    /// <summary>
    /// Checks if an entity exists by ID.
    /// </summary>
    public abstract Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Deletes all entities matching a filter.
    /// </summary>
    public abstract Task<int> DeleteAsync(Func<T, bool> predicate);

    /// <summary>
    /// Gets paginated results filtered by tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to filter by</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="orderBy">The property to order by (e.g., "CreatedAt DESC")</param>
    /// <returns>A paginated result with items and total count</returns>
    public abstract Task<PagedResult<T>> GetPageAsync(string tenantId, int pageNumber, int pageSize, string? orderBy = null);

    /// <summary>
    /// Gets paginated results.
    /// </summary>
    public virtual async Task<PaginatedResult<T>> GetPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var all = await GetAllAsync();
            var totalCount = all.Count;
            var items = all
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

            return new PaginatedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting paged results: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Bulk creates multiple entities.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when entities is null.</exception>
    public virtual async Task<int> BulkCreateAsync(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        int count = 0;

        try
        {
            foreach (var entity in entities)
            {
                await CreateAsync(entity);
                count++;
            }

            _logger.LogInformation("Bulk created {Count} entities", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in bulk create: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Bulk updates multiple entities.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when entities is null.</exception>
    public virtual async Task<int> BulkUpdateAsync(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        int count = 0;

        try
        {
            foreach (var entity in entities)
            {
                if (await UpdateAsync(entity))
                {
                    count++;
                }
            }

            _logger.LogInformation("Bulk updated {Count} entities", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in bulk update: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Bulk deletes multiple entities by IDs.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when ids is null.</exception>
    public virtual async Task<int> BulkDeleteAsync(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        int count = 0;

        try
        {
            foreach (var id in ids)
            {
                if (await DeleteAsync(id))
                {
                    count++;
                }
            }

            _logger.LogInformation("Bulk deleted {Count} entities", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in bulk delete: {Message}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Immutable page of results together with paging metadata.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="PageNumber">The current page number (1-based).</param>
/// <param name="PageSize">The number of items per page.</param>
/// <param name="TotalPages">The total number of pages.</param>
public sealed record PagedResult<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages) where T : class
{
    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Mutable page of results together with paging metadata.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class PaginatedResult<T> where T : class
{
    /// <summary>
    /// Gets or sets the items on the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Returns a string summary of the paging state without the item contents.
    /// </summary>
    /// <returns>A string containing the item count and paging values.</returns>
    public override string ToString()
    {
        return $"PaginatedResult {{ Items = {Items.Count}, TotalCount = {TotalCount}, PageNumber = {PageNumber}, PageSize = {PageSize}, TotalPages = {TotalPages} }}";
    }
}

/// <summary>
/// Unit of work pattern for coordinating multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <returns>The number of changes saved.</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Starts a new transaction.
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the transaction in progress.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no transaction is in progress.</exception>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the transaction in progress.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no transaction is in progress.</exception>
    Task RollbackAsync();
}

/// <summary>
/// Default <see cref="IUnitOfWork"/> implementation that tracks transaction state.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;
    private bool _transactionStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record unit of work activity.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public UnitOfWork(ILogger<UnitOfWork> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _transactionStarted = false;
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            // Implementation would persist changes
            _logger.LogInformation("Changes saved");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error saving changes: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync()
    {
        try
        {
            _transactionStarted = true;
            _logger.LogInformation("Transaction started");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error starting transaction: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CommitAsync()
    {
        try
        {
            if (!_transactionStarted)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            _transactionStarted = false;
            _logger.LogInformation("Transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error committing transaction: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RollbackAsync()
    {
        try
        {
            if (!_transactionStarted)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            _transactionStarted = false;
            _logger.LogInformation("Transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error rolling back transaction: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_transactionStarted)
        {
            try
            {
                _transactionStarted = false;
                _logger.LogInformation("Transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error rolling back transaction: {Message}", ex.Message);
            }
        }
    }
}
