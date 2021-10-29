# TenantContextHelper

Central utility for managing tenant-scoped ambient context in multi-tenant SQLite applications. It provides thread-safe storage and retrieval of tenant-specific data, enabling consistent tenant resolution across layers without explicit parameter passing.

## API

### `TenantContextHelper()`
Constructor. Initializes a new instance of the helper with an empty tenant context.

### `public void SetTenantContext(TenantContext context)`
Establishes the ambient tenant context for the current logical operation.
- **context**: The tenant context to set.
- Throws `ArgumentNullException` if `context` is `null`.

### `public TenantContext GetTenantContext()`
Retrieves the current tenant context from the ambient scope.
- Returns the active `TenantContext`, or `null` if none is set.

### `public bool HasTenantContext()`
Determines whether a tenant context is currently active.
- Returns `true` if a context is set; otherwise, `false`.

### `public string GetCurrentTenantId()`
Gets the ID of the currently active tenant.
- Returns the tenant ID as a string, or `null` if no context is active.
- Throws `InvalidOperationException` if no tenant context is present.

### `public void ClearTenantContext()`
Removes the current tenant context from the ambient scope.

### `public bool ValidateTenantContext()`
Validates that a tenant context is currently active and usable.
- Returns `true` if a valid context exists; otherwise, `false`.

### `public IDisposable CreateScope()`
Creates a new disposable scope for the current tenant context.
- Returns an `IDisposable` scope that restores the previous context upon disposal.

### `public Dictionary<string, object> GetContextMetadata()`
Retrieves the metadata dictionary associated with the current tenant context.
- Returns a new `Dictionary<string, object>` containing the current context’s metadata, or an empty dictionary if no context is set.

### `public string EnrichErrorWithContext(Exception ex)`
Decorates an exception message with tenant context information for improved diagnostics.
- **ex**: The exception to enrich.
- Returns a formatted string combining the original exception message with tenant context details.
- Throws `ArgumentNullException` if `ex` is `null`.

### `public sealed class TenantContextScope`
A disposable scope that manages tenant context lifetimes. Implements `IDisposable`.

### `public void Dispose()`
Disposes the current tenant context scope, restoring the previous context if applicable. Part of `IDisposable`.
