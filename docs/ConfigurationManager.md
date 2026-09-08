# ConfigurationManager

`ConfigurationManager` is the sealed implementation of [`IConfigurationManager`](IConfigurationManager.md). It provides a thread-serialized, case-insensitive in-memory key/value store and, when created with the full constructor, access to an injected `Microsoft.Extensions.Configuration.IConfiguration` plus validated `MultiTenantOptions`.

The two stores are separate: `Get`, `Set`, `TryGet`, `Remove`, `Contains`, `GetAll`, `LoadFromDictionary`, and `ExportConfiguration` use the manager's private in-memory dictionary. `GetSection` and `GetTenantSetting` read the injected `IConfiguration`; they do not read values added with `Set` or `LoadFromDictionary`.

```csharp
public sealed class ConfigurationManager : IConfigurationManager
```

## Constructors

### Full constructor

```csharp
public ConfigurationManager(
    Microsoft.Extensions.Configuration.IConfiguration configuration,
    ILogger<ConfigurationManager> logger,
    Microsoft.Extensions.Options.IOptions<MultiTenantOptions> multiTenantOptions)
```

Creates a manager with both an external configuration source and validated multi-tenant options. `configuration`, `logger`, and `multiTenantOptions` must be non-null. The constructor reads `multiTenantOptions.Value` and validates these option properties:

- `DefaultMaxConnections` must be greater than zero.
- `BasePath` must be non-null and non-empty, and its directory must already exist.

It throws `ArgumentNullException` for a null dependency or null options value, `ArgumentOutOfRangeException` for an invalid `DefaultMaxConnections`, `ArgumentException` for an empty `BasePath`, and `DirectoryNotFoundException` when `BasePath` does not exist.

### In-memory constructor

```csharp
public ConfigurationManager(ILogger<ConfigurationManager> logger)
```

Creates a manager that supports the in-memory operations only. Because this constructor does not supply `IConfiguration` or `MultiTenantOptions`, calls to `GetSection`, `GetTenantSetting`, or `GetMultiTenantOptions` throw `InvalidOperationException`.

## Public methods

### GetSection

```csharp
public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
```

Returns `IConfiguration.GetSection(key)` from the injected external configuration. The key is a configuration section path, conventionally colon-delimited, such as `"Database:Main"`. The actual source may be JSON, environment variables, command-line values, or any other provider used to build the injected `IConfiguration`. A missing key normally produces an empty section. Throws `InvalidOperationException` when the in-memory-only constructor was used.

### GetTenantSetting

```csharp
public string? GetTenantSetting(string tenantId, string key)
```

Reads the external configuration in this order:

1. `Tenants:{tenantId}:Settings:{key}`
2. `GlobalSettings:{key}`

The global value is used when the tenant value is null or empty. The method returns null if neither key has a value. Throws `InvalidOperationException` when the in-memory-only constructor was used.

### GetMultiTenantOptions

```csharp
public MultiTenantOptions GetMultiTenantOptions()
```

Returns the same validated `MultiTenantOptions` instance obtained from `IOptions<MultiTenantOptions>.Value` by the full constructor. Throws `InvalidOperationException` when the in-memory-only constructor was used.

### Get&lt;T&gt;

```csharp
public T Get<T>(string key, T defaultValue)
```

Reads `key` from the private in-memory dictionary. If the key is absent, conversion fails, or an exception occurs, it returns `defaultValue`. Stored values are returned directly when already assignable to `T`; otherwise `Convert.ChangeType` is attempted.

### Set&lt;T&gt;

```csharp
public void Set<T>(string key, T value)
```

Adds or replaces an in-memory value. Passing null removes the key. Keys are compared using `StringComparer.OrdinalIgnoreCase`.

### TryGet&lt;T&gt;

```csharp
public bool TryGet<T>(string key, out T? value)
```

Attempts to read an in-memory value. It returns `true` when the stored object is assignable to `T`, or when `Convert.ChangeType` produces a non-null `T`. It returns `false` and assigns the default value when the key is absent or conversion fails.

### Remove

```csharp
public void Remove(string key)
```

Removes an in-memory key. Removing a missing key has no effect.

### Contains

```csharp
public bool Contains(string key)
```

Returns whether the private in-memory dictionary contains `key`, using case-insensitive comparison.

### GetAll

```csharp
public Dictionary<string, object> GetAll()
```

Returns a new case-insensitive dictionary containing all current in-memory entries. Mutating the returned dictionary does not alter the manager.

### LoadFromDictionary

```csharp
public void LoadFromDictionary(Dictionary<string, object> settings)
```

Adds or overwrites every entry from `settings` in the private in-memory dictionary. It does not clear keys that are absent from `settings` and does not update the injected `IConfiguration`.

### ExportConfiguration

```csharp
public Dictionary<string, object> ExportConfiguration()
```

Returns the same kind of snapshot as `GetAll`. It exports only the private in-memory entries, not values from the injected external configuration.

## Relationship to IConfigurationManager

`IConfigurationManager` declares `Get<T>(string key, T defaultValue)`, `Set<T>`, `TryGet<T>`, `Remove`, `Contains`, and `GetAll`; these can be used through an interface reference. The constructors and the methods `GetSection`, `GetTenantSetting`, `GetMultiTenantOptions`, `LoadFromDictionary`, and `ExportConfiguration` are members of the concrete `ConfigurationManager` only.

The interface's operations all target the in-memory dictionary. Consumers that need the injected configuration hierarchy or tenant/global fallback must depend on the concrete class or introduce another abstraction for those concrete-only methods.

## Usage example

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqliteMultiTenant.Configuration;
using TenantConfigurationManager = SqliteMultiTenant.Configuration.ConfigurationManager;

var basePath = Path.Combine(Path.GetTempPath(), "sqlite-tenants");
Directory.CreateDirectory(basePath);

IConfiguration applicationConfiguration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["GlobalSettings:Theme"] = "light",
        ["Tenants:tenant-42:Settings:Theme"] = "dark"
    })
    .Build();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var options = Options.Create(new MultiTenantOptions
{
    BasePath = basePath,
    DefaultMaxConnections = 20
});

var manager = new TenantConfigurationManager(
    applicationConfiguration,
    loggerFactory.CreateLogger<TenantConfigurationManager>(),
    options);

// Reads Tenants:tenant-42:Settings:Theme, with GlobalSettings:Theme as fallback.
string? theme = manager.GetTenantSetting("tenant-42", "Theme");

// The interface exposes the separate in-memory store.
IConfigurationManager values = manager;
values.Set("BatchSize", 250);
int batchSize = values.Get("BatchSize", 100);
bool hasBatchSize = values.Contains("batchsize"); // true: keys are case-insensitive
Dictionary<string, object> snapshot = values.GetAll();
```
