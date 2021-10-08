# ConfigurationExtensions

Extension methods and utilities for working with `IConfiguration` in multi-tenant SQLite applications. Provides safe value retrieval, environment variable overrides, configuration validation, and section binding with support for multiple configuration sources.

## API

### `public static T GetValueSafe<T>(this IConfiguration config, string key)`
Retrieves a configuration value safely, returning the default value for type `T` if the key is missing or the value is invalid.

- **Parameters**
  - `config`: The configuration instance.
  - `key`: The configuration key to retrieve.
- **Returns**: The parsed value of type `T`, or default(`T`) if not found or invalid.
- **Throws**: None. Returns default on failure.

---

### `public static string GetRequiredValue(this IConfiguration config, string key)`
Retrieves a configuration value and throws if the key is missing or the value is empty.

- **Parameters**
  - `config`: The configuration instance.
  - `key`: The configuration key to retrieve.
- **Returns**: The configuration value.
- **Throws**: `InvalidOperationException` if the key is missing or the value is empty.

---

### `public static T BindSection<T>(this IConfiguration config, string sectionKey) where T : new()`
Binds a configuration section to a new instance of type `T`.

- **Parameters**
  - `config`: The configuration instance.
  - `sectionKey`: The section key to bind.
- **Returns**: A new instance of `T` with values bound from the configuration section.
- **Throws**: None. Returns a new instance even if the section is missing.

---

### `public static string GetConnectionStringSafe(this IConfiguration config, string name)`
Retrieves a connection string safely, returning `null` if the connection string is missing.

- **Parameters**
  - `config`: The configuration instance.
  - `name`: The connection string name.
- **Returns**: The connection string if found; otherwise, `null`.
- **Throws**: None.

---

### `public static bool HasValue(this IConfiguration config, string key)`
Checks whether a configuration key exists and has a non-empty value.

- **Parameters**
  - `config`: The configuration instance.
  - `key`: The configuration key to check.
- **Returns**: `true` if the key exists and the value is non-empty; otherwise, `false`.
- **Throws**: None.

---
### `public static Dictionary<string, string> GetSectionAsDictionary(this IConfiguration config, string sectionKey)`
Converts a configuration section into a dictionary of key-value pairs.

- **Parameters**
  - `config`: The configuration instance.
  - `sectionKey`: The section key to convert.
- **Returns**: A dictionary representing the section's key-value pairs.
- **Throws**: None. Returns an empty dictionary if the section is missing.

---
### `public static IEnumerable<string> ValidateConfiguration(this IConfiguration config)`
Validates the configuration by checking for required keys and connection strings.

- **Parameters**
  - `config`: The configuration instance.
- **Returns**: An enumerable of validation error messages. Empty if validation passes.
- **Throws**: None.

---
### `public static void Reload(this IConfiguration config)`
Reloads the underlying configuration sources.

- **Parameters**
  - `config`: The configuration instance.
- **Returns**: None.
- **Throws**: None.

---
### `public static string GetValueWithEnvironmentOverride(this IConfiguration config, string key)`
Retrieves a configuration value, allowing environment variables to override it.

- **Parameters**
  - `config`: The configuration instance.
  - `key`: The configuration key to retrieve.
- **Returns**: The configuration value, or the environment variable value if present.
- **Throws**: None.

---
### `public sealed class ConfigurationBuilder`
A sealed builder for creating `IConfigurationRoot` instances with support for multiple sources.

---
### `public ConfigurationBuilder()`
Initializes a new instance of the `ConfigurationBuilder`.

- **Parameters**: None.
- **Returns**: A new `ConfigurationBuilder` instance.

---
### `public ConfigurationBuilder AddJsonFile(this ConfigurationBuilder builder, string path)`
Adds a JSON file to the configuration sources.

- **Parameters**
  - `builder`: The configuration builder.
  - `path`: The path to the JSON file.
- **Returns**: The `ConfigurationBuilder` instance for chaining.
- **Throws**: None.

---
### `public ConfigurationBuilder AddEnvironmentVariables(this ConfigurationBuilder builder, string prefix = null)`
Adds environment variables to the configuration sources.

- **Parameters**
  - `builder`: The configuration builder.
  - `prefix`: Optional prefix to filter environment variables.
- **Returns**: The `ConfigurationBuilder` instance for chaining.
- **Throws**: None.

---
### `public ConfigurationBuilder AddInMemory(this ConfigurationBuilder builder, IEnumerable<KeyValuePair<string, string>> memoryCollection)`
Adds in-memory key-value pairs to the configuration sources.

- **Parameters**
  - `builder`: The configuration builder.
  - `memoryCollection`: The in-memory key-value pairs.
- **Returns**: The `ConfigurationBuilder` instance for chaining.
- **Throws**: None.

---
### `public IConfigurationRoot Build(this ConfigurationBuilder builder)`
Builds the configuration from the registered sources.

- **Parameters**
  - `builder`: The configuration builder.
- **Returns**: The built `IConfigurationRoot`.
- **Throws**: None.

---
### `public static IConfigurationRoot BuildStandardConfiguration()`
Builds a standard configuration with JSON, environment variables, and in-memory sources.

- **Parameters**: None.
- **Returns**: The built `IConfigurationRoot`.
- **Throws**: None.

## Usage

### Example 1: Safe Configuration Retrieval
