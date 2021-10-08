# IConfigurationManager

`IConfigurationManager` provides a centralized, type-safe abstraction for managing application configuration in a multi-tenant SQLite environment. It exposes methods to retrieve, update, validate, and export configuration values, including tenant-specific overrides and strongly-typed section mapping. The concrete implementation, `ConfigurationManager`, wraps `Microsoft.Extensions.Configuration` primitives and adds multi-tenancy awareness through `MultiTenantOptions` and tenant-keyed settings.

## API

### ConfigurationManager

```csharp
public sealed class ConfigurationManager : IConfigurationManager
```

Concrete implementation of `IConfigurationManager`. Sealed to prevent further derivation.

---

### ConfigurationManager Constructor

```csharp
public ConfigurationManager(/* dependencies injected */)
```

Constructs a new `ConfigurationManager` instance. Dependencies are injected via the constructor and are not part of the public surface documented here.

---

### GetSection

```csharp
public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
```

Retrieves a raw configuration section by its key.

- **Parameters**: `key` — the section path (e.g., `"Database:Main"`).
- **Returns**: an `IConfigurationSection` that can be further traversed or bound.
- **Throws**: never throws; returns an empty section if the key does not exist.

---

### GetTenantSetting

```csharp
public string? GetTenantSetting(string tenantId, string settingKey)
```

Returns a tenant-specific setting value, or `null` if no override exists for that tenant and key.

- **Parameters**:
  - `tenantId` — the tenant identifier.
  - `settingKey` — the configuration key to look up under the tenant’s scope.
- **Returns**: the setting value as a string, or `null`.
- **Throws**: does not throw.

---

### GetMultiTenantOptions

```csharp
public MultiTenantOptions GetMultiTenantOptions()
```

Returns the current multi-tenant options that govern tenant resolution, default connection strategies, and isolation policies.

- **Returns**: a populated `MultiTenantOptions` instance.
- **Throws**: may throw if the underlying configuration section is missing required fields and cannot be bound.

---

### Get\<T\>

```csharp
public T Get<T>(string key)
```

Deserializes the configuration subtree at `key` into an instance of `T`.

- **Parameters**: `key` — the configuration path to bind.
- **Returns**: a new instance of `T` populated from the configuration values.
- **Throws**: `InvalidOperationException` or binding exceptions if the section cannot be mapped to `T`.

---

### Set\<T\>

```csharp
public void Set<T>(string key, T value)
```

Writes a value into the in-memory configuration at the specified key. This does not persist to disk or external stores.

- **Parameters**:
  - `key` — the configuration path.
  - `value` — the object to store; complex types are decomposed into nested keys.
- **Returns**: void.
- **Throws**: may throw if `value` is `null` and the underlying provider does not accept nulls.

---

### TryGet\<T\>

```csharp
public bool TryGet<T>(string key, out T value)
```

Attempts to bind the configuration at `key` to `T`. Returns `true` if the section exists and binding succeeds; otherwise `false` and `value` is set to `default(T)`.

- **Parameters**:
  - `key` — the configuration path.
  - `value` — the bound result or `default(T)`.
- **Returns**: `true` on success; `false` otherwise.
- **Throws**: does not throw.

---

### Remove

```csharp
public void Remove(string key)
```

Removes a key and all its children from the in-memory configuration.

- **Parameters**: `key` — the path to remove.
- **Returns**: void.
- **Throws**: does not throw; silently no-ops if the key does not exist.

---

### Contains

```csharp
public bool Contains(string key)
```

Checks whether a configuration key exists.

- **Parameters**: `key` — the path to test.
- **Returns**: `true` if the key is present; `false` otherwise.
- **Throws**: does not throw.

---

### GetAll

```csharp
public Dictionary<string, object> GetAll()
```

Flattens the entire configuration tree into a dictionary of string keys and object values.

- **Returns**: a dictionary where each key is a colon-separated path and each value is the terminal configuration value.
- **Throws**: does not throw.

---

### LoadFromDictionary

```csharp
public void LoadFromDictionary(Dictionary<string, object> source)
```

Replaces the current in-memory configuration with the entries from the provided dictionary. Existing keys not present in the dictionary are removed.

- **Parameters**: `source` — the dictionary to load.
- **Returns**: void.
- **Throws**: `ArgumentNullException` if `source` is `null`.

---

### ExportConfiguration

```csharp
public Dictionary<string, object> ExportConfiguration()
```

Exports the current in-memory configuration as a flat dictionary. Equivalent to `GetAll()` but may include additional metadata depending on implementation.

- **Returns**: a dictionary of all configuration entries.
- **Throws**: does not throw.

---

### ConfigurationValidator

```csharp
public sealed class ConfigurationValidator
```

A sealed utility class that provides static validation methods for common configuration values used in the multi-tenant SQLite setup.

---

### ValidateConnectionString

```csharp
public bool ValidateConnectionString(string connectionString)
```

Validates that a connection string is well-formed and contains the minimum required elements for a SQLite connection.

- **Parameters**: `connectionString` — the candidate connection string.
- **Returns**: `true` if valid; `false` otherwise.
- **Throws**: does not throw.

---

### ValidatePort

```csharp
public bool ValidatePort(int port)
```

Checks that a port number falls within the valid range (1–65535).

- **Parameters**: `port` — the port number to validate.
- **Returns**: `true` if valid; `false` otherwise.
- **Throws**: does not throw.

---

### ValidateRetentionDays

```csharp
public bool ValidateRetentionDays(int days)
```

Validates that a retention period in days is a positive integer and does not exceed a reasonable upper bound.

- **Parameters**: `days` — the retention period.
- **Returns**: `true` if valid; `false` otherwise.
- **Throws**: does not throw.

---

### ValidateFilePath

```csharp
public bool ValidateFilePath(string path)
```

Checks that a file path is syntactically valid and the directory portion exists (if applicable).

- **Parameters**: `path` — the file path to validate.
- **Returns**: `true` if valid; `false` otherwise.
- **Throws**: does not throw.

---

### AppConfiguration

```csharp
public sealed class AppConfiguration
```

A sealed POCO that holds the full set of application-level configuration values, including database paths, tenant defaults, and operational parameters. Typically obtained by calling `Get<AppConfiguration>("App")`.

## Usage

### Example 1: Reading and Validating Configuration on Startup

```csharp
var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

// Retrieve the strongly-typed application configuration
var appConfig = configManager.Get<AppConfiguration>("App");

// Validate critical values before proceeding
var validator = new ConfigurationValidator();
if (!validator.ValidateConnectionString(appConfig.DefaultConnectionString))
{
    throw new InvalidOperationException("Default connection string is invalid.");
}
if (!validator.ValidateFilePath(appConfig.DatabaseDirectory))
{
    throw new InvalidOperationException("Database directory path is invalid.");
}

// Access multi-tenant options
var mtOptions = configManager.GetMultiTenantOptions();
Console.WriteLine($"Tenant resolution strategy: {mtOptions.ResolutionStrategy}");
```

### Example 2: Setting and Exporting Tenant-Specific Overrides

```csharp
var configManager = serviceProvider.GetRequiredService<IConfigurationManager>();

// Set a tenant-specific retention override
configManager.Set("Tenants:TenantA:RetentionDays", 90);

// Verify the override is present
if (configManager.TryGet<int>("Tenants:TenantA:RetentionDays", out var retentionDays))
{
    Console.WriteLine($"TenantA retention: {retentionDays} days");
}

// Export the full configuration for diagnostics
var exported = configManager.ExportConfiguration();
foreach (var kvp in exported)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

## Notes

- **In-Memory Only**: `Set<T>`, `Remove`, and `LoadFromDictionary` operate exclusively on the in-memory representation. They do not persist changes to JSON files, environment variables, or other backing stores. Restarting the process reverts to the original sources.
- **Tenant Key Semantics**: `GetTenantSetting` relies on a convention where tenant overrides are stored under a `Tenants:<tenantId>` prefix. The exact prefix is determined by `MultiTenantOptions`.
- **Thread Safety**: The underlying `IConfiguration` providers are not thread-safe for mutation. Concurrent calls to `Set<T>`, `Remove`, or `LoadFromDictionary` must be externally synchronized to avoid race conditions. Read-only methods (`Get<T>`, `TryGet<T>`, `Contains`, `GetAll`, `ExportConfiguration`) are safe to call concurrently with each other but not with concurrent writes.
- **Validation Methods**: `ConfigurationValidator` methods are static checks only; they do not verify actual runtime availability (e.g., `ValidateFilePath` does not guarantee the file exists, only that the path is well-formed and the directory is present).
- **Null Handling**: `GetTenantSetting` returns `null` for missing overrides. `Set<T>` with a `null` value may be rejected depending on the underlying provider. Prefer `Remove` to clear a key.
- **Export vs GetAll**: Both methods produce a flat dictionary. `ExportConfiguration` may include additional internal markers; treat it as the canonical snapshot for diagnostics and migration.
