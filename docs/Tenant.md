# Tenant

Represents a tenant within the multi-tenant SQLite architecture. A `Tenant` encapsulates identity, lifecycle status, connection limits, metadata, and references to its isolated or shared database resources. It serves as the central entity for tenant management, access tracking, and configuration.

## API

### Properties

#### `string TenantId`
The unique identifier for the tenant. This value is immutable after creation and is used as the primary key for all tenant-scoped operations.

#### `string Name`
The human-readable display name of the tenant. Typically used in administrative interfaces and logs.

#### `string? Description`
An optional free-text description of the tenant’s purpose, business unit, or ownership context. May be `null`.

#### `TenantStatus Status`
The current lifecycle state of the tenant. The `TenantStatus` enumeration controls whether the tenant can accept connections and perform data operations.

#### `DateTime CreatedAt`
The UTC timestamp when the tenant record was first persisted. Set once at creation and not modified thereafter.

#### `DateTime UpdatedAt`
The UTC timestamp of the most recent modification to any property on the tenant record. Updated automatically on changes.

#### `DateTime? LastAccessedAt`
The UTC timestamp of the last recorded access to the tenant’s data, set by `MarkAsAccessed()`. `null` if the tenant has never been accessed.

#### `string? ContactEmail`
An optional email address for the tenant’s administrative or technical contact. May be `null`.

#### `string? DatabasePath`
The file-system path to the tenant’s primary SQLite database file when `IsDataIsolated` is `true`. `null` for shared-database tenants.

#### `bool IsDataIsolated`
Indicates whether this tenant uses a dedicated, physically separate SQLite database file (`true`) or shares a database with other tenants (`false`). Determines the meaning of `DatabasePath`.

#### `int MaxConnections`
The maximum number of concurrent connections permitted for this tenant. Enforcement is handled externally by the connection pool or middleware.

#### `Dictionary<string, string>? Metadata`
A flexible key-value store for arbitrary tenant metadata. May be `null`. Keys and values are strings. Manipulated via `SetMetadata()`.

#### `ICollection<TenantDatabase> Databases`
The collection of `TenantDatabase` records associated with this tenant. Represents all database files, both primary and auxiliary, owned by the tenant.

#### `ICollection<TenantSettings> Settings`
The collection of `TenantSettings` records containing key-value configuration pairs specific to this tenant.

#### `bool Validate`
Returns `true` if the tenant’s current state and configuration pass internal consistency checks; otherwise `false`. Does not throw.

### Methods

#### `void MarkAsAccessed()`
Updates `LastAccessedAt` to the current UTC time and persists the change. Call this when a tenant’s database is opened or queried.

- **Throws:** `InvalidOperationException` if the tenant’s `Status` is not a state that permits access (e.g., `Deactivated`).

#### `void Deactivate()`
Transitions the tenant’s `Status` to `Deactivated`, preventing future access attempts. Updates `UpdatedAt`.

- **Throws:** `InvalidOperationException` if the tenant is already in a terminal state that forbids deactivation.

#### `void Activate()`
Transitions the tenant’s `Status` to `Active`, allowing connection and query operations to proceed. Updates `UpdatedAt`.

- **Throws:** `InvalidOperationException` if the tenant is in a state that cannot be reactivated.

#### `void SetMetadata(string key, string value)`
Inserts or updates a single key-value pair in the `Metadata` dictionary. If `Metadata` is `null`, a new dictionary is instantiated before the entry is added.

- **Parameters:**
  - `key`: A non-null, non-empty string.
  - `value`: The string value to associate with the key. May be `null`.
- **Throws:** `ArgumentNullException` if `key` is `null`; `ArgumentException` if `key` is empty or whitespace.

## Usage

### Example 1: Creating and Activating an Isolated Tenant

```csharp
var tenant = new Tenant
{
    TenantId = "tenant-abc-123",
    Name = "Acme Corporation",
    Description = "Primary tenant for Acme's production data",
    ContactEmail = "admin@acme.example",
    IsDataIsolated = true,
    DatabasePath = "/data/tenants/abc-123/main.db",
    MaxConnections = 10
};

// Tenant starts in an inactive state; activate before use.
tenant.Activate();

// Record initial access.
tenant.MarkAsAccessed();

Console.WriteLine($"Tenant {tenant.Name} is {tenant.Status}, last accessed at {tenant.LastAccessedAt}");
```

### Example 2: Managing Metadata and Lifecycle

```csharp
// Retrieve an existing tenant from the store.
Tenant existing = tenantStore.GetById("tenant-xyz-456");

// Attach custom metadata.
existing.SetMetadata("region", "eu-west");
existing.SetMetadata("tier", "premium");
existing.SetMetadata("backup_window", "02:00-04:00 UTC");

// Validate configuration before use.
if (!existing.Validate)
{
    Console.WriteLine("Tenant configuration is invalid; aborting.");
    return;
}

// Deactivate when contract ends.
existing.Deactivate();

// Verify deactivation prevents access.
try
{
    existing.MarkAsAccessed();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Access blocked: {ex.Message}");
}
```

## Notes

- **Thread Safety:** Instance members are not thread-safe. External synchronisation is required if a `Tenant` instance is shared across threads, particularly when calling `MarkAsAccessed()`, `Activate()`, `Deactivate()`, or `SetMetadata()` concurrently.
- **Lifecycle State Machine:** The valid transitions for `Status` are governed by the `TenantStatus` enumeration and enforced by `Activate()` and `Deactivate()`. Calling these methods from an invalid current state throws `InvalidOperationException`. Consult the `TenantStatus` documentation for the complete state diagram.
- **`MarkAsAccessed()` and Status:** This method throws if the tenant is not in a state that permits data access. Always check `Status` or handle the exception when recording access on tenants that may be deactivated.
- **`SetMetadata()` Key Rules:** Keys are case-sensitive. Passing a `null` or whitespace-only key throws immediately. A `null` value is stored as-is; removing a key is not supported through this method and must be done by directly manipulating the `Metadata` dictionary if exposed.
- **`Validate` Behaviour:** The property performs a synchronous, non-throwing check of internal invariants (e.g., `IsDataIsolated` requiring a non-null `DatabasePath`, `MaxConnections` being positive). It does not check external state such as file existence or database integrity.
- **`Databases` and `Settings` Collections:** These are navigation properties typically loaded on demand by the persistence layer. They may be empty but are never `null` when the tenant is materialised from storage.
