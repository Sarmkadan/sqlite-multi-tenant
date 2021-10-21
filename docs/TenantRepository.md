# TenantRepository
The `TenantRepository` provides asynchronous data‑access operations for the `Tenant` entity in a SQLite‑backed multi‑tenant application. It implements `ITenantRepository` and is intended to be instantiated via dependency injection or directly, encapsulating all interactions with the underlying tenant table.

## API
### Constructor
```csharp
public TenantRepository()
```
Initializes a new instance of the repository. The constructor configures the internal SQLite connection; any required connection string or DbContext should be supplied through the application’s composition root.

### GetByIdAsync
```csharp
public async Task<Tenant?> GetByIdAsync(object id)
```
Retrieves a single tenant by its unique identifier.  
- **Parameters**  
  - `id`: The tenant’s primary key value.  
- **Return value**  
  - A `Task` that completes with the matching `Tenant` instance, or `null` if no tenant exists with the specified id.  
- **Exceptions**  
  - Throws `SQLiteException` if a database error occurs.  
  - Throws `ObjectDisposedException` if the repository has been disposed.  

### GetByNameAsync
```csharp
public async Task<Tenant?> GetByNameAsync(string name)
```
Retrieves a tenant by its name.  
- **Parameters**  
  - `name`: The exact name of the tenant to locate.  
- **Return value**  
  - A `Task` that completes with the matching `Tenant`, or `null` when no tenant matches the name.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `name` is `null`.  
  - Throws `SQLiteException` on database access failures.  

### GetAllAsync
```csharp
public async Task<List<Tenant>> GetAllAsync()
```
Returns every tenant stored in the database.  
- **Return value**  
  - A `Task` that completes with a list containing all `Tenant` records. The list may be empty but will never be `null`.  
- **Exceptions**  
  - Throws `SQLiteException` for query execution problems.  

### GetActiveTenantsAsync
```csharp
public async Task<List<Tenant>> GetActiveTenantsAsync()
```
Returns tenants whose status indicates they are active.  
- **Return value**  
  - A `Task` that completes with a list of active `Tenant` objects.  
- **Exceptions**  
  - Throws `SQLiteException` if the underlying query fails.  

### GetByStatusAsync
```csharp
public async Task<List<Tenant>> GetByStatusAsync(TenantStatus status)
```
Returns tenants that match a specific status value.  
- **Parameters**  
  - `status`: The `TenantStatus` enumeration value to filter by.  
- **Return value**  
  - A `Task` that completes with a list of tenants having the requested status.  
- **Exceptions**  
  - Throws `ArgumentOutOfRangeException` if `status` is not a defined `TenantStatus`.  
  - Throws `SQLiteException` on database errors.  

### AddAsync
```csharp
public async Task<Tenant> AddAsync(Tenant tenant)
```
Inserts a new tenant record and returns the persisted entity, including any generated identifier.  
- **Parameters**  
  - `tenant`: The `Tenant` instance to add. Its identifier property should be unset or default.  
- **Return value**  
  - A `Task` that completes with the supplied `Tenant` populated with the database‑generated key.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `tenant` is `null`.  
  - Throws `SQLiteException` for insert failures (e.g., constraint violations).  

### UpdateAsync
```csharp
public async Task UpdateAsync(Tenant tenant)
```
Updates an existing tenant record with the values from the supplied instance.  
- **Parameters**  
  - `tenant`: The `Tenant` containing updated values; must include a valid identifier.  
- **Return value**  
  - A `Task` that completes when the update operation finishes.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `tenant` is `null`.  
  - Throws `InvalidOperationException` if no tenant with the given identifier exists.  
  - Throws `SQLiteException` on update errors.  

### DeleteAsync
```csharp
public async Task DeleteAsync(object id)
```
Removes the tenant with the specified identifier from the database.  
- **Parameters**  
  - `id`: The primary key of the tenant to delete.  
- **Return value**  
  - A `Task` that completes when the deletion is finished.  
- **Exceptions**  
  - Throws `SQLiteException` if the delete statement fails.  
  - Throws `ObjectDisposedException` if the repository has been disposed.  

### ExistsAsync
```csharp
public async Task<bool> ExistsAsync(object id)
```
Checks whether a tenant with the given identifier exists.  
- **Parameters**  
  - `id`: The tenant identifier to test.  
- **Return value**  
  - A `Task` that completes with `true` if a matching tenant is found, otherwise `false`.  
- **Exceptions**  
  - Throws `SQLiteException` for query execution problems.  

### GetTotalCountAsync
```csharp
public async Task<int> GetTotalCountAsync()
```
Returns the total number of tenant rows in the table.  
- **Return value**  
  - A `Task` that completes with an integer count (≥ 0).  
- **Exceptions**  
  - Throws `SQLiteException` if the count query fails.  

### SearchAsync
```csharp
public async Task<List<Tenant>> SearchAsync(string term)
```
Performs a case‑insensitive search across tenant name (and other searchable fields) for the supplied term.  
- **Parameters**  
  - `term`: The string to search for; may be empty to return all tenants.  
- **Return value**  
  - A `Task` that completes with a list of matching `Tenant` objects.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `term` is `null`.  
  - Throws `SQLiteException` on query errors.  

### GetPagedAsync
```csharp
public async Task<List<Tenant>> GetPagedAsync(int pageIndex, int pageSize)
```
Retrieves a subset of tenants using zero‑based paging.  
- **Parameters**  
  - `pageIndex`: The zero‑based page number to fetch.  
  - `pageSize`: The maximum number of tenants per page; must be greater than zero.  
- **Return value**  
  - A `Task` that completes with the list of tenants for the requested page. May be empty if the page lies beyond the data set.  
- **Exceptions**  
  - Throws `ArgumentOutOfRangeException` if `pageIndex` is negative or `pageSize` is ≤ 0.  
  - Throws `SQLiteException` if the paged query cannot be executed.  

## Usage
### Example 1: Basic CRUD flow
```csharp
using var repo = new TenantRepository();

// Add a new tenant
var newTenant = new Tenant { Name = "Acme Corp", IsActive = true };
var added = await repo.AddAsync(newTenant);
Console.WriteLine($"Added tenant with Id {added.Id}");

// Retrieve the tenant by name
var fetched = await repo.GetByNameAsync("Acme Corp");
if (fetched != null)
{
    fetched.IsActive = false;
    await repo.UpdateAsync(fetched);
}

// Delete the tenant
await repo.DeleteAsync(added.Id);
```

### Example 2: Searching and paging
```csharp
using var repo = new TenantRepository();

// Find tenants containing "Corp" in the name
var matches = await repo.SearchAsync("Corp");
Console.WriteLine($"Found {matches.Count} matching tenants");

// Show the second page of results, 10 items per page
var page = await repo.GetPagedAsync(pageIndex: 1, pageSize: 10);
foreach (var t in page)
{
    Console.WriteLine($"{t.Id}: {t.Name}");
}
```

## Notes
- The repository does not maintain mutable state; all methods rely on the underlying SQLite connection. Consequently, multiple threads may invoke its methods concurrently provided the connection itself is thread‑safe (the default SQLite connection used here is safe for concurrent reads but requires serialization for writes).  
- Passing `null` for any reference‑type parameter (`name`, `tenant`, `search term`) results in an `ArgumentNullException`.  
- Methods that accept an identifier (`GetByIdAsync`, `ExistsAsync`, `DeleteAsync`) assume the identifier type matches the primary key column; supplying an incompatible type will cause a runtime exception from the SQLite provider.  
- If the repository is disposed (e.g., when scoped to a DI container and the scope ends), any further call will throw `ObjectDisposedException`.  
- All asynchronous methods propagate cancellation tokens only if explicitly supplied by the caller; otherwise they run to completion or fail with the exceptions described above.  
- The repository does not automatically manage transactions; callers needing atomic operations across multiple repository calls should wrap those calls in a `using var transaction = await connection.BeginTransactionAsync()` block or use a higher‑level unit‑of‑work pattern.
