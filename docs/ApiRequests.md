# API request DTOs

This document catalogs every request DTO declared in `src/Api/Requests/ApiRequests.cs`. None of the properties in that file has a validation attribute; where validation occurs, it is performed outside the DTO. For more detailed descriptions of the first five request types, see [CreateTenantRequest.md](CreateTenantRequest.md).

## `CreateTenantRequest`

Consumed by `TenantController.CreateTenantAsync(CreateTenantRequest request)`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `Name` | `string` | None |
| `Description` | `string` | None |
| `ContactEmail` | `string` | None |

## `UpdateTenantRequest`

Consumed by `TenantController.UpdateTenantAsync(string tenantId, UpdateTenantRequest request)`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `Name` | `string` | None |
| `Description` | `string` | None |
| `ContactEmail` | `string` | None |

## `CreateMigrationRequest`

Consumed by `MigrationController.CreateMigrationAsync(CreateMigrationRequest request)`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `DatabaseId` | `string` | None |
| `Version` | `string` | None |
| `Name` | `string` | None |
| `UpScript` | `string` | None |
| `DownScript` | `string` | None |

## `QueryMigrationsRequest`

Not currently consumed by an action in `src/Api/Controllers`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `DatabaseId` | `string` | None |
| `Status` | `string` | None |
| `Limit` | `int` | None |
| `Offset` | `int` | None |

## `RestoreBackupRequest`

Not currently consumed by an action in `src/Api/Controllers`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `BackupId` | `string` | None |
| `TargetDatabaseId` | `string` | None |
| `ConfirmRestore` | `bool` | None |
| `RestoredBy` | `string` | None |

## `PaginationRequest`

Not currently consumed by an action in `src/Api/Controllers`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `PageNumber` | `int` | None |
| `PageSize` | `int` | None |

`GetOffset()` is a helper method, not a request property.

## `BatchOperationRequest`

Not currently consumed by an action in `src/Api/Controllers`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `ResourceIds` | `List<string>` | None |
| `Operation` | `string` | None |
| `Parameters` | `Dictionary<string, object>` | None |

## `WebhookSubscriptionRequest`

Not currently consumed by an action in `src/Api/Controllers`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `Url` | `string` | None |
| `EventType` | `string` | None |
| `Enabled` | `bool` | None |
| `Headers` | `Dictionary<string, string>` | None |

## `ApplyMigrationsToMultipleRequest`

Consumed by `MigrationController.ApplyMigrationsToMultipleDatabasesAsync(ApplyMigrationsToMultipleRequest request)`.

| Property | Type | Validation attributes |
| --- | --- | --- |
| `DatabaseIds` | `List<string>` | None |
| `AppliedBy` | `string` | None |
