# Service Interfaces

This document lists the service contracts defined in `src/Services`. Signatures are reproduced exactly from their interfaces.

## `IBackupService`

Implemented by `BackupService`.

```csharp
Task<Backup?> GetBackupAsync(string backupId, CancellationToken cancellationToken = default);

Task<List<Backup>> GetDatabaseBackupsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default);

Task<Backup> CreateBackupAsync(string databaseId, BackupType backupType, string createdBy, string? backupPath = null, CancellationToken cancellationToken = default);

Task MarkBackupAsCompletedAsync(string backupId, long sizeBytes, long durationMs, CancellationToken cancellationToken = default);

Task MarkBackupAsFailedAsync(string backupId, string errorMessage, CancellationToken cancellationToken = default);

Task<BackupVerificationResult> VerifyBackupAsync(string backupId, string verifiedBy, CancellationToken cancellationToken = default);

Task SetBackupExpirationAsync(string backupId, DateTime expirationDate, CancellationToken cancellationToken = default);

Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default);

Task<int> GetBackupCountAsync(string databaseId, CancellationToken cancellationToken = default);

Task DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);

Task AddBackupTagAsync(string backupId, string tag, CancellationToken cancellationToken = default);

Task BackupWithProgressAsync(
    string sourceDatabasePath,
    string destinationPath,
    IProgress<BackupProgress>? progress = null,
    int pagesPerStep = -1,
    CancellationToken cancellationToken = default);
```

## `IMigrationService`

Implemented by `MigrationService`.

```csharp
Task<Migration?> GetMigrationAsync(string migrationId, CancellationToken cancellationToken = default);

Task<List<Migration>> GetDatabaseMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<Migration> CreateMigrationAsync(string databaseId, string version, string name, string upScript, string? downScript = null, CancellationToken cancellationToken = default);

Task<MigrationResult> ExecuteMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);

Task<MigrationResult> RollbackMigrationAsync(string migrationId, string executedBy, CancellationToken cancellationToken = default);

Task<MigrationResult> MarkMigrationAsCompletedAsync(string migrationId, long executionTimeMs, CancellationToken cancellationToken = default);

Task<MigrationResult> MarkMigrationAsFailedAsync(string migrationId, string errorMessage, CancellationToken cancellationToken = default);

Task<int> GetMigrationCountAsync(string databaseId, CancellationToken cancellationToken = default);

Task<bool> IsMigrationAppliedAsync(string databaseId, string version, CancellationToken cancellationToken = default);

Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);

Task<Models.MigrationBatchResult> ApplyMigrationsWithFaultIsolationAsync(string databaseId, string executedBy, CancellationToken cancellationToken = default);

Task<Models.MigrationBatchResult> ApplyMigrationsToMultipleDatabasesAsync(
    List<string> databaseIds,
    string executedBy,
    CancellationToken cancellationToken = default);
```

## `ITenantService`

Implemented by `TenantService`.

```csharp
Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task<Tenant> CreateTenantAsync(string name, string? description = null, string? contactEmail = null, CancellationToken cancellationToken = default);

Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);

Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

Task ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task SuspendTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);

Task<int> GetTenantCountAsync(CancellationToken cancellationToken = default);

Task<List<Tenant>> SearchTenantsAsync(string searchTerm, CancellationToken cancellationToken = default);

Task SetTenantMetadataAsync(string tenantId, string key, string value, CancellationToken cancellationToken = default);

Task<TenantStorageInfo> GetTenantDatabaseSizeAsync(string tenantId, CancellationToken cancellationToken = default);
```

## `ITenantDatabaseMaintenanceService`

Implemented by `TenantDatabaseMaintenanceService`.

```csharp
Task<TenantMaintenanceResult> VacuumTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantMaintenanceResult>> VacuumAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

Task<TenantMaintenanceResult> AnalyzeTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantMaintenanceResult>> AnalyzeAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

Task<TenantMaintenanceResult> OptimizeTenantDatabaseAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantMaintenanceResult>> OptimizeAllTenantDatabasesAsync(CancellationToken cancellationToken = default);

Task<TenantMaintenanceResult> PerformFullMaintenanceAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantMaintenanceResult>> PerformFullMaintenanceOnAllAsync(CancellationToken cancellationToken = default);
```

## `ITenantSizeReportService`

Implemented by `TenantSizeReportService`.

```csharp
Task<TenantSizeReportRecord> GenerateReportForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantSizeReportRecord>> GenerateReportForAllTenantsAsync(CancellationToken cancellationToken = default);

Task<string> GenerateTextTableReportAsync(CancellationToken cancellationToken = default);

Task<string> GenerateCompleteReportAsync(CancellationToken cancellationToken = default);
```

## `IIntegrityCheckService`

Implemented by `IntegrityCheckService`.

```csharp
Task<TenantIntegrityCheckResult> CheckTenantIntegrityAsync(string tenantId, CancellationToken cancellationToken = default);

Task<List<TenantIntegrityCheckResult>> CheckTenantsIntegrityAsync(
    IEnumerable<string> tenantIds,
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);

Task<List<TenantIntegrityCheckResult>> CheckAllTenantsIntegrityAsync(
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);

Task<List<TenantIntegrityCheckResult>> CheckActiveTenantsIntegrityAsync(
    int maxDegreeOfParallelism = 4,
    CancellationToken cancellationToken = default);
```
