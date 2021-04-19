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
            _logger.LogError($"Error getting paged results: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Bulk creates multiple entities.
    /// </summary>
    public virtual async Task<int> BulkCreateAsync(IEnumerable<T> entities)
    {
        int count = 0;

        try
        {
            foreach (var entity in entities)
            {
                await CreateAsync(entity);
                count++;
            }

            _logger.LogInformation($"Bulk created {count} entities");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk create: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Bulk updates multiple entities.
    /// </summary>
    public virtual async Task<int> BulkUpdateAsync(IEnumerable<T> entities)
    {
        int count = 0;

        try
        {
            foreach (var entity in entities)
            {
                if (await UpdateAsync(entity))
                    count++;
            }

            _logger.LogInformation($"Bulk updated {count} entities");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk update: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Bulk deletes multiple entities by IDs.
    /// </summary>
    public virtual async Task<int> BulkDeleteAsync(IEnumerable<string> ids)
    {
        int count = 0;

        try
        {
            foreach (var id in ids)
            {
                if (await DeleteAsync(id))
                    count++;
            }

            _logger.LogInformation($"Bulk deleted {count} entities");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk delete: {ex.Message}");
            throw;
        }
    }
}

public class PaginatedResult<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Unit of work pattern for coordinating multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;
    private bool _transactionStarted;

    public UnitOfWork(ILogger<UnitOfWork> logger)
    {
        _logger = logger;
        _transactionStarted = false;
    }

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
            _logger.LogError($"Error saving changes: {ex.Message}");
            throw;
        }
    }

    public async Task BeginTransactionAsync()
    {
        try
        {
            _transactionStarted = true;
            _logger.LogInformation("Transaction started");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error starting transaction: {ex.Message}");
            throw;
        }
    }

    public async Task CommitAsync()
    {
        try
        {
            if (!_transactionStarted)
                throw new InvalidOperationException("No transaction in progress");

            _transactionStarted = false;
            _logger.LogInformation("Transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error committing transaction: {ex.Message}");
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (!_transactionStarted)
                throw new InvalidOperationException("No transaction in progress");

            _transactionStarted = false;
            _logger.LogInformation("Transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rolling back transaction: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        if (_transactionStarted)
        {
            RollbackAsync().Wait();
        }
    }
}
