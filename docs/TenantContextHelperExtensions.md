# TenantContextHelperExtensions

Provides extension methods for managing and validating tenant context in multi-tenant SQLite applications. These helpers simplify tenant-scoped operations by ensuring tenant context is properly initialized, validated, and cleaned up.

## API

### `CreateValidatedScope`
Creates a new tenant context scope that validates the current tenant context. The scope ensures the tenant context is valid for the duration of the operation and automatically disposes of the context when the scope ends.

- **Return value**: An `IDisposable` scope that manages the tenant context lifetime.
- **Throws**: `InvalidOperationException` if the tenant context is invalid when the scope is created.

### `GetRequiredTenantId`
Retrieves the tenant ID from the current tenant context. This method asserts that a tenant context is active and returns its ID.

- **Return value**: The tenant ID as a `string`.
- **Throws**: `InvalidOperationException` if no tenant context is active.

### `ExecuteInTenantContext`
Executes an action within the context of the specified tenant. The tenant context is set for the duration of the action and automatically cleaned up afterward.

- **Parameters**:
  - `tenantId` (`string`): The tenant identifier.
  - `action` (`Action`): The action to execute within the tenant context.
- **Throws**: `ArgumentNullException` if `tenantId` or `action` is `null`.
- **Throws**: `InvalidOperationException` if tenant context setup fails.

### `ExecuteInTenantContext<T>`
Executes a function within the context of the specified tenant and returns its result. The tenant context is set for the duration of the function and automatically cleaned up afterward.

- **Parameters**:
  - `tenantId` (`string`): The tenant identifier.
  - `func` (`Func<T>`): The function to execute within the tenant context.
- **Return value**: The result of the function.
- **Throws**: `ArgumentNullException` if `tenantId` or `func` is `null`.
- **Throws**: `InvalidOperationException` if tenant context setup fails.

### `GetRequiredTenantContext`
Retrieves the current tenant context. This method asserts that a tenant context is active and returns it.

- **Return value**: The current `TenantContext`.
- **Throws**: `InvalidOperationException` if no tenant context is active.

### `IsCurrentTenant`
Checks whether the specified tenant ID matches the current tenant context.

- **Parameters**:
  - `tenantId` (`string`): The tenant identifier to compare.
- **Return value**: `true` if the tenant ID matches the current tenant context; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `tenantId` is `null`.

## Usage
