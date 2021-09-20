# TenantSettings

`TenantSettings` is a configuration class used in a multi-tenant SQLite application to store and manage tenant-specific settings. It encapsulates key-value pairs with metadata for type-safe retrieval, encryption support, and lifecycle tracking.

## API

### Properties

- **`SettingId`** (string)
  Unique identifier for the setting. Used as the primary key in storage.

- **`TenantId`** (string)
  Identifier of the tenant this setting belongs to. Links the setting to a specific tenant.

- **`SettingKey`** (string)
  The key under which the setting is stored or retrieved. Typically used as a configuration key.

- **`SettingValue`** (string)
  The raw string value of the setting. May be encrypted depending on `IsEncrypted`.

- **`Description`** (string?, optional)
  Human-readable explanation of the setting's purpose or usage.

- **`DataType`** (string?, optional)
  Indicates the expected data type of the setting (e.g., `"int"`, `"bool"`, `"json"`). Used by `GetValue<T>` for type conversion.

- **`IsEncrypted`** (bool)
  Flag indicating whether the `SettingValue` is stored encrypted. If `true`, `SettingValue` is encrypted; otherwise, it is stored in plaintext.

- **`CreatedAt`** (DateTime)
  Timestamp when the setting was first created.

- **`UpdatedAt`** (DateTime)
  Timestamp of the last update to the setting.

- **`LastModifiedBy`** (string?, optional)
  Identifier of the user or system that last modified the setting.

- **`IsActive`** (bool)
  Indicates whether the setting is currently active and should be applied.

- **`Tenant`** (Tenant?, optional)
  Navigation property referencing the associated tenant. May be `null` if not loaded.

- **`Validate`** (bool)
  Flag controlling whether the setting should be validated during retrieval or update. When `true`, enforces type and constraint checks.

### Methods

- **`UpdateValue<T>(T newValue)`**
  Updates the setting's value with a new typed value.
  - **Type Parameter**: `T` – The type of the new value.
  - **Parameters**: `newValue` – The new value to store.
  - **Behavior**: Converts `newValue` to a string, stores it in `SettingValue`, sets `UpdatedAt` to current UTC time, and updates `DataType` based on `typeof(T)`.
  - **Throws**: `InvalidOperationException` if `Validate` is `true` and `newValue` cannot be converted to a string or violates constraints.
  - **Note**: If `IsEncrypted` is `true`, the value is encrypted before storage.

- **`SetActive(bool active)`**
  Activates or deactivates the setting.
  - **Parameters**: `active` – `true` to activate, `false` to deactivate.
  - **Behavior**: Sets `IsActive` and updates `UpdatedAt` to current UTC time.

- **`GetValue<T>()`**
  Retrieves the setting value as a strongly-typed instance.
  - **Type Parameter**: `T` – The expected return type.
  - **Returns**: The parsed value of type `T`.
  - **Throws**:
    - `InvalidOperationException` if `SettingValue` is `null` or empty.
    - `FormatException` if the value cannot be parsed to `T`.
    - `InvalidCastException` if `DataType` does not match the expected type and `Validate` is `true`.
  - **Note**: If `IsEncrypted` is `true`, the value is decrypted before parsing.

- **`SetValue<T>(T value)`**
  Sets the raw string value of the setting directly.
  - **Type Parameter**: `T` – The type of the value being set.
  - **Parameters**: `value` – The value to store as a string.
  - **Behavior**: Assigns `value.ToString()` to `SettingValue`, updates `DataType` based on `typeof(T)`, and sets `UpdatedAt` to current UTC time.
  - **Throws**: `ArgumentNullException` if `value` is `null`.
  - **Note**: Does not perform encryption. Use `UpdateValue<T>` for encrypted storage.

## Usage

### Example 1: Storing and Retrieving a Plaintext Setting
