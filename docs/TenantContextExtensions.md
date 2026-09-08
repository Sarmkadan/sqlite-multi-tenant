# TenantContextExtensions and TenantContextBuilder

`TenantContextExtensions` adds typed context-data access, conditional context-data insertion, active-state checking, and compact summaries to `TenantContext`. `TenantContextBuilder` provides a fluent way to populate and validate a `TenantContext`.

Both types are in the `SqliteMultiTenant.Models` namespace.

## Extension methods

### `GetTypedContextData<T>(string key)`

Retrieves `key` from `ContextData` when the stored value is assignable to `T`.

- Returns the typed value when the key exists and its value is a `T`.
- Returns `default(T)` when `ContextData` is `null`, the key is absent, or the value has a different type. The method does not perform type conversion.
- Throws `ArgumentNullException` when the context is `null`.
- Throws `ArgumentException` when `key` is `null` or empty.

### `SetContextDataIfAbsent(string key, object value)`

Adds a context-data entry only when `key` is not already present. If `ContextData` is `null`, the method initializes it through `TenantContext.SetContextData`.

- Returns `true` when the entry is added.
- Returns `false` when the key already exists; the existing value is preserved.
- Throws `ArgumentNullException` when the context or `value` is `null`.
- Throws `ArgumentException` when `key` is `null` or empty.

### `IsActive()`

Returns `true` when `IsValid` is `true` and `TenantId` is not null, empty, or whitespace. Returns `false` otherwise.

Throws `ArgumentNullException` when the context is `null`.

### `ToSummaryString()`

Returns an invariant, pipe-delimited summary in this format:

```text
TenantId|TenantName|UserEmail
```

Missing tenant names and user email addresses are represented by `N/A`. `TenantId` is emitted as stored.

Throws `ArgumentNullException` when the context is `null`.

## Builder methods

Every configuration method returns the same `TenantContextBuilder` instance, so calls can be chained.

### `WithTenantId(string tenantId)`

Sets `TenantId`. A null, empty, or whitespace value is rejected later by `Build()`.

### `WithTenantName(string? tenantName)`

Sets the optional human-readable tenant name.

### `WithUserId(string? userId)`

Sets the optional user identifier.

### `WithUserEmail(string? userEmail)`

Sets the optional user email address.

### `WithEstablishedAt(DateTime establishedAt)`

Sets the time at which the tenant was established.

### `WithCreatedAt(DateTime createdAt)`

Sets the context creation time, replacing the default assigned by `TenantContext`.

### `WithRequestId(string? requestId)`

Sets the optional request or operation identifier.

### `WithConnectionId(string? connectionId)`

Sets the optional database connection identifier.

### `WithDatabasePath(string? databasePath)`

Sets the optional path to the tenant database.

### `WithContextData(Dictionary<string, object>? contextData)`

Assigns the supplied dictionary to `ContextData`, replacing the current dictionary reference. Passing `null` clears the property. The dictionary is not copied, so later changes to it are visible through the built context.

### `WithAllowedTenants(IEnumerable<string> allowedTenants)`

Adds every supplied tenant identifier to the context's `AllowedTenants` set. Existing entries remain, and duplicates are ignored by the underlying `HashSet<string>`. Passing a null sequence results in `NullReferenceException` when it is enumerated.

### `AsInvalid()`

Calls `TenantContext.Invalidate()` and returns the builder. Because the builder exposes no method that restores `IsValid`, calling `Build()` afterward throws `InvalidOperationException`.

### `Build()`

Validates and returns the builder's `TenantContext` instance. It does not create a copy.

Validation fails when `TenantId` is null, empty, or whitespace, or when the context has been marked invalid. On failure, `Build()` throws `InvalidOperationException` with the validation error prefixed by `Failed to build TenantContext:`.

Repeated calls return the same mutable `TenantContext` instance. Reusing the builder after a successful build therefore also modifies the previously returned context.

## Builder example

```csharp
using System;
using System.Collections.Generic;
using SqliteMultiTenant.Models;

var contextData = new Dictionary<string, object>
{
    ["region"] = "eu-west"
};

TenantContext context = new TenantContextBuilder()
    .WithTenantId("tenant-42")
    .WithTenantName("Contoso")
    .WithUserId("user-7")
    .WithUserEmail("user@contoso.example")
    .WithEstablishedAt(DateTime.UtcNow.AddYears(-2))
    .WithCreatedAt(DateTime.UtcNow)
    .WithRequestId("request-123")
    .WithConnectionId("connection-9")
    .WithDatabasePath("data/tenant-42.db")
    .WithContextData(contextData)
    .WithAllowedTenants(new[] { "tenant-42", "tenant-archive" })
    .Build();

string? region = context.GetTypedContextData<string>("region");
bool added = context.SetContextDataIfAbsent("plan", "enterprise");
bool active = context.IsActive();
string summary = context.ToSummaryString();
// tenant-42|Contoso|user@contoso.example
```

`AsInvalid()` is useful when constructing a context that must remain unusable, but such a context cannot be returned through `Build()`:

```csharp
var invalidBuilder = new TenantContextBuilder()
    .WithTenantId("tenant-42")
    .AsInvalid();

// Throws InvalidOperationException because the context is marked as invalid.
invalidBuilder.Build();
```
