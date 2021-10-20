# SettingsController

Provides HTTP endpoints for managing application settings in a multi-tenant SQLite environment. Supports CRUD operations for individual and batch settings updates, type-safe value handling, and runtime configuration inspection.

## API

### `SettingsController`
Initializes a new instance of the `SettingsController` class.

### `GetAllSettings`
Returns all application settings.

- **Returns**
  - `200 OK` with a list of `SettingValue` objects representing all settings.
  - `500 Internal Server Error` if the operation fails.

### `GetSetting(string key)`
Retrieves the value of a specific setting by its key.

- **Parameters**
  - `key` (string): The unique identifier of the setting to retrieve.
- **Returns**
  - `200 OK` with the `SettingValue` if the setting exists.
  - `404 Not Found` if the setting does not exist.
  - `500 Internal Server Error` if the operation fails.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.

### `SetSetting(SetSettingRequest request)`
Creates or updates a single application setting.

- **Parameters**
  - `request` (`SetSettingRequest`): The request containing the setting key and value.
- **Returns**
  - `201 Created` if the setting was created.
  - `200 OK` if the setting was updated.
  - `400 Bad Request` if the request is invalid (e.g., invalid type or missing key).
  - `500 Internal Server Error` if the operation fails.
- **Throws**
  - `ArgumentNullException` if `request` or `request.Key` is `null`.

### `UpdateBatchSettings(IEnumerable<SetSettingRequest> requests)`
Applies multiple setting updates in a single transaction.

- **Parameters**
  - `requests` (`IEnumerable<SetSettingRequest>`): A collection of setting updates to apply.
- **Returns**
  - `200 OK` with a `BatchSettingUpdateResult` indicating success and failure counts, including any errors.
  - `400 Bad Request` if the request collection is `null` or empty.
  - `500 Internal Server Error` if the operation fails.
- **Throws**
  - `ArgumentNullException` if `requests` is `null`.

### `RemoveSetting(string key)`
Deletes a specific application setting.

- **Parameters**
  - `key` (string): The unique identifier of the setting to remove.
- **Returns**
  - `204 No Content` if the setting was removed.
  - `404 Not Found` if the setting does not exist.
  - `500 Internal Server Error` if the operation fails.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.

### `CheckSetting(string key)`
Verifies whether a specific setting exists.

- **Parameters**
  - `key` (string): The unique identifier of the setting to check.
- **Returns**
  - `200 OK` with a boolean indicating existence if the setting exists.
  - `404 Not Found` if the setting does not exist.
  - `500 Internal Server Error` if the operation fails.
- **Throws**
  - `ArgumentNullException` if `key` is `null`.

### `GetAppInfo`
Returns metadata about the application and its runtime environment.

- **Returns**
  - `200 OK` with an object containing application name, version, environment, and tenant identifier.

## Usage

### Example 1: Retrieve and Update a Setting
