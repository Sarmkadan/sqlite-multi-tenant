# Repository Interfaces

The repository contracts are defined in the `SqliteMultiTenant.Repositories` namespace. Each contract is implemented by the concrete SQLite repository named in its section below.

## `IBackupRepository`

Source: `src/Repositories/IBackupRepository.cs`  
Implemented by: `BackupRepository` (`src/Repositories/BackupRepository.cs`)

```csharp
Task<List<Backup>> GetAllAsync(CancellationToken cancellationToken = default);
Task<Backup?> GetByIdAsync(string backupId, CancellationToken cancellationToken = default);
Task<List<Backup>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Backup>> GetVerifiedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Backup>> GetFailedBackupsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default);
Task<Backup> AddAsync(Backup backup, CancellationToken cancellationToken = default);
Task UpdateAsync(Backup backup, CancellationToken cancellationToken = default);
Task DeleteAsync(string backupId, CancellationToken cancellationToken = default);
Task<bool> ExistsAsync(string backupId, CancellationToken cancellationToken = default);
Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default);
Task<List<Backup>> GetPagedAsync(string databaseId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```

## `IMigrationRepository`

Source: `src/Repositories/IMigrationRepository.cs`  
Implemented by: `MigrationRepository` (`src/Repositories/MigrationRepository.cs`)

```csharp
Task<List<Migration>> GetAllAsync(CancellationToken cancellationToken = default);
Task<Migration?> GetByIdAsync(string migrationId, CancellationToken cancellationToken = default);
Task<List<Migration>> GetByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Migration>> GetPendingMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Migration>> GetAppliedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Migration>> GetFailedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
Task<Migration?> GetByVersionAsync(string databaseId, string version, CancellationToken cancellationToken = default);
Task<Migration> AddAsync(Migration migration, CancellationToken cancellationToken = default);
Task UpdateAsync(Migration migration, CancellationToken cancellationToken = default);
Task DeleteAsync(string migrationId, CancellationToken cancellationToken = default);
Task<bool> ExistsAsync(string migrationId, CancellationToken cancellationToken = default);
Task<int> GetCountByDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);
Task<List<Migration>> GetOrderedMigrationsAsync(string databaseId, CancellationToken cancellationToken = default);
```

## `ITenantRepository`

Source: `src/Repositories/ITenantRepository.cs`  
Implemented by: `TenantRepository` (`src/Repositories/TenantRepository.cs`)

```csharp
Task<Tenant?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);
Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
Task<List<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
Task<List<Tenant>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
Task DeleteAsync(string tenantId, CancellationToken cancellationToken = default);
Task<bool> ExistsAsync(string tenantId, CancellationToken cancellationToken = default);
Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
Task<List<Tenant>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
Task<List<Tenant>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```
