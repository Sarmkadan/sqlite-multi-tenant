# TenantContext

`TenantContext` is a sealed class that encapsulates tenant-specific and request-specific contextual data for multi-tenant applications using SQLite. It provides a centralized store for tenant identifiers, user information, request metadata, and custom context data, enabling consistent access to tenant-scoped state throughout the application lifecycle.

## API

### `public string TenantId`
Gets or sets the unique identifier for the current tenant. This value is used to scope data and operations to a specific tenant. Must not be null or empty when `IsValid` is `true`.

### `public string? TenantName`
Gets or sets the human-readable name of the tenant. Optional; may be null if not provided.

### `public string? UserId`
Gets or sets the identifier of the authenticated user associated with the current request. Optional; may be null if the request is anonymous.

### `public string? UserEmail`
Gets or sets the email address of the authenticated user. Optional; may be null if not provided or if the user has no associated email.

### `public DateTime EstablishedAt`
Gets or sets the date and time when the tenant was established. Used for auditing and lifecycle management. Must be a valid date in the past.

### `public DateTime CreatedAt`
Gets or sets the date and time when this `TenantContext` instance was created. Automatically set when the instance is constructed.

### `public string? RequestId`
Gets or sets a unique identifier for the current HTTP request or operation. Optional; may be null if not provided by the caller.

### `public string? ConnectionId`
Gets or sets an identifier for the database connection associated with this context. Optional; may be null if not applicable.

### `public string? DatabasePath`
Gets or sets the filesystem path to the tenant-specific SQLite database. Optional; may be null if the database is in-memory or the path is not applicable.

### `public Dictionary<string, object>? ContextData`
Gets or sets a dictionary of custom key-value pairs associated with the current tenant or request. Optional; may be null if no custom data is stored.

### `public bool IsValid`
Gets a value indicating whether the context contains valid tenant data. Returns `true` only if `TenantId` is non-null and `Validate()` has been called without errors.

### `public bool Validate()`
Validates the current state of the context. Returns `true` if all required fields are valid; otherwise, returns `false` and marks the context as invalid via `Invalidate()`. Required fields include a non-null `TenantId` and a valid `EstablishedAt` date.

### `public object? GetContextData(string key)`
Retrieves the value associated with the specified key from `ContextData`.

- **Parameters**: `key` — The key whose value to retrieve.
- **Returns**: The value associated with `key`, or `null` if the key does not exist or `ContextData` is `null`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public void SetContextData(string key, object value)`
Stores a key-value pair in `ContextData`.

- **Parameters**:
  - `key` — The key under which to store the value.
  - `value` — The value to store.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public void Invalidate()`
Marks the context as invalid by setting `IsValid` to `false`. This clears all non-essential state and indicates that the context should not be used for tenant-scoped operations.

### `public override string ToString()`
Returns a string representation of the context, including `TenantId`, `TenantName`, `UserId`, and `IsValid`. Useful for logging and debugging.

## Usage

### Example 1: Initializing and Validating a TenantContext
