# TenantValidation

## Overview

`TenantValidation` provides extension methods for validating `Tenant` instances. Validation is non-mutating: `Validate` returns every error it finds, `IsValid` reduces that result to a Boolean, and `EnsureValid` throws when any errors are present.

## Public Type

```csharp
public static class TenantValidation
```

The static class is declared in the `SqliteMultiTenant.Models` namespace. Its methods extend `Tenant`.

## Public Methods

### `Validate`

```csharp
public static IReadOnlyList<string> Validate(this Tenant value)
```

Validates `value` and returns a read-only list containing all detected validation errors. The list is empty when the tenant is valid. Passing `null` throws `ArgumentNullException`.

It applies these rules:

- `TenantId` is required and cannot be null, empty, or whitespace-only. When present, its length cannot exceed `TenantConstants.MaxTenantIdLength`, which is **36 characters**.
- `Name` is required and cannot be null, empty, or whitespace-only. When present, its length cannot exceed `TenantConstants.MaxTenantNameLength`, which is **128 characters**.
- `Status` must be a value defined by the `TenantStatus` enum. An undefined numeric enum value is invalid.
- `CreatedAt` cannot be `default(DateTime)`. It also cannot be later than five minutes after the UTC time at which that check runs.
- `UpdatedAt` cannot be `default(DateTime)`. It also cannot be later than five minutes after the UTC time at which that check runs.
- `CreatedAt` cannot be later than `UpdatedAt`.
- `LastAccessedAt` is optional. When it has a value, that value cannot be later than five minutes after the UTC time at which the check runs.
- `ContactEmail` is optional. When it is non-null:
  - It cannot be empty or whitespace-only.
  - It cannot exceed **254 characters**.
  - It must match the case-insensitive regular expression `^[^@\s]+@[^@\s]+\.[^@\s]+$`: one or more non-whitespace, non-`@` characters, followed by `@`, another such sequence, a literal `.`, and a final such sequence.
- `DatabasePath` is optional. When it is non-null, it cannot be empty or whitespace-only and its length cannot exceed `TenantConstants.MaxDatabasePathLength`, which is **260 characters**.
- `MaxConnections` must be greater than **0** and cannot exceed **1,000**.
- `Metadata` is optional. When it is non-null:
  - It cannot contain more than **1,000 entries**.
  - Every key must be non-null, non-empty, and not whitespace-only.
  - Every key cannot exceed **128 characters**.
  - A metadata value may be null, but a non-null value cannot exceed **1,024 characters**.
- `Databases` cannot be null and cannot contain any null entry.
- `Settings` cannot be null and cannot contain any null entry.

There are no validation rules for `Description` or `IsDataIsolated`. This validator also does not validate the contents of non-null `TenantDatabase` or `TenantSettings` objects; it only checks their collections for null references.

For a field whose checks form an `else if` chain, only the first applicable field-level error is added. Errors from different fields and collection checks accumulate in the returned list.

### `IsValid`

```csharp
public static bool IsValid(this Tenant value)
```

Calls `Validate` and returns `true` when the returned list contains no errors; otherwise it returns `false`. Because validation is delegated to `Validate`, passing `null` throws `ArgumentNullException`, and all rules listed above apply.

### `EnsureValid`

```csharp
public static void EnsureValid(this Tenant value)
```

Applies all rules listed under `Validate`. It returns normally when there are no errors. Passing `null` throws `ArgumentNullException`. If validation produces any errors, it throws `ArgumentException` with the errors joined by `; ` and prefixed with `Tenant validation failed: `.

## Usage Example

```csharp
using SqliteMultiTenant.Models;

var now = DateTime.UtcNow;
var tenant = new Tenant
{
    TenantId = "tenant-42",
    Name = "Example tenant",
    CreatedAt = now,
    UpdatedAt = now,
    ContactEmail = "owner@example.com",
    MaxConnections = 25
};

IReadOnlyList<string> errors = tenant.Validate();

if (errors.Count > 0)
{
    Console.WriteLine(string.Join(Environment.NewLine, errors));
}

if (tenant.IsValid())
{
    tenant.EnsureValid(); // Returns normally; invalid tenants cause ArgumentException.
}
```
