# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2027-01-10
### Added
- Add async bulk import/export with streaming and progress reporting
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

## [1.0.0] - 2025-11-10

### Added
- Stable public release of SQLite Multi-Tenant
- Comprehensive documentation suite (getting-started, architecture, deployment guides)
- 5 example applications covering basic setup through advanced operations
- Docker support with Dockerfile and docker-compose.yml
- GitHub Actions CI/CD pipeline (build, test, NuGet publish, CodeQL)
- NuGet packaging with README embed and source link

### Changed
- Promoted all beta APIs to stable; finalized public surface
- Locked down `MultiTenantOptions` property names for 1.x compatibility

### Fixed
- Race condition when two threads first-open the same tenant database simultaneously
- `ArchiveTenantAsync` now validates tenant exists before state transition

## [0.9.0] - 2025-10-27

### Added
- BenchmarkDotNet suite for tenant validation, string operations, and query builder hot paths
- `FrozenSet<string>` for O(1) reserved-ID lookup in `TenantNameValidator`
- `ArrayPool<byte>` buffer reuse in SHA-256/MD5 hash helpers
- `StringBuilder`-based `QueryBuilder.Build()` replacing LINQ + string interpolation

### Changed
- `RegexOptions.Compiled` applied to all static patterns in `StringUtilities`
- `SanitizeForFilePath` rewritten as single-pass `ArrayPool<char>` write

### Fixed
- Edge case: tenant names consisting entirely of whitespace now rejected
- `MigrationService` no longer throws `NullReferenceException` when `DownScript` is omitted
- Backup expiration query included backups with `null` `ExpiresAt`; now correctly excluded

## [0.8.0] - 2025-10-13

### Added
- Background workers: `BackupScheduler`, `DatabaseMaintenanceWorker`, `DataRetentionPolicy`, `BackupRotationManager`
- `AuditLogger` with configurable retention and trend analysis
- `MetricsService` and `StatisticsService` for real-time counters and aggregates
- `PerformanceMonitor` for per-operation timing
- `ReportGenerator` producing structured diagnostic reports

### Changed
- `HealthCheckService` now surfaces per-tenant database reachability in addition to system-level status

### Fixed
- `BackupRotationManager` leaked file handles when an expired backup file was already deleted on disk

## [0.7.0] - 2025-09-29

### Added
- `CacheService` (in-memory LRU with TTL) and `DistributedCacheService` abstractions
- `EncryptionService` (AES-256-CBC) and `EncryptionKeyManager`
- `RateLimiter` with token-bucket algorithm
- `HealthCheckService` with system diagnostics endpoint
- `DataConsistencyChecker` for cross-database integrity validation
- `DataExporter` (JSON, CSV) and `DataImporter` with conflict resolution

### Fixed
- Cache eviction under concurrent reads could produce duplicate entries; now lock-free via `ConcurrentDictionary`

## [0.6.0] - 2025-09-15

### Added
- `IEventBus` / `EventBusImpl` pub-sub with async handlers
- Domain events: `TenantCreatedEvent`, `TenantStatusChangedEvent`, `BackupCompletedEvent`
- `WebhookService` and `WebhookHandler` for outbound HTTP event delivery
- `ScheduledTaskService` with cron-style timer support
- `MultiTenantHttpClientFactory` for per-tenant `HttpClient` instances

### Changed
- `EventPublisher` now batches delivery failures and retries up to three times with exponential backoff

## [0.5.0] - 2025-09-01

### Added
- REST API controllers: `TenantController`, `DatabaseController`, `MigrationController`, `BackupController`, `AdminController`, `SettingsController`
- `RequestInterceptor` for authentication enforcement
- `CorrelationIdMiddleware`, `LoggingMiddleware`, `ErrorHandlingMiddleware`, `PerformanceMiddleware`, `RateLimitingMiddleware`
- `ApiResponseBuilder` and `ResultWrapper<T>` for uniform response envelope
- `RequestResponseLogger` with configurable body capture

### Changed
- All service methods now accept `CancellationToken` consistently

### Fixed
- `ErrorHandlingMiddleware` swallowed inner exception details in production mode; now logs full chain at Debug level

## [0.4.0] - 2025-08-18

### Added
- CLI interface: `CliApplication`, `CommandLineParser`, `CommandParser`, `CommandExecutor`
- Commands for all tenant, database, migration, backup, and system operations
- `OutputFormatter` with JSON, CSV, and XML renderers
- `BatchOperationHandler` and `BulkInsertBuilder` for high-volume writes
- `ConflictResolutionService` with configurable strategies (Skip, Overwrite, Merge)

### Changed
- `ConnectionManager` now pre-warms connections during `AddSqliteMultiTenant` registration

## [0.3.0] - 2025-08-04

### Added
- `IBackupService` / `BackupService` with Full, Incremental, and Differential backup types
- Backup verification, expiration policy, and tagging system
- `IBackupRepository` / `BackupRepository` backed by SQLite master database
- `BackupScheduler` skeleton integrated into `IHostedService`
- `BackupException` for backup-specific error paths

### Fixed
- `MigrationService.RollbackMigrationAsync` left status as `Applied` when `DownScript` execution threw; now correctly sets `Failed`

## [0.2.0] - 2025-07-21

### Added
- `IMigrationService` / `MigrationService`: create, execute, rollback, history
- `IMigrationRepository` / `MigrationRepository` with pending/applied queries
- `SchemaManager` for raw DDL execution against tenant databases
- `ConnectionPoolManager` and `ConnectionPoolOptions`
- `GenericRepository<T>` with query builder integration
- `MigrationException` for migration-specific error paths

### Changed
- `TenantRepository` switched from direct `SqliteConnection` to pooled `ConnectionManager`

### Fixed
- `TenantService.SearchTenantsAsync` returned duplicates when search term matched both `Name` and `Description`

## [0.1.0] - 2025-07-07

### Added
- Initial release of SQLite Multi-Tenant
- Core tenant management
  - `Tenant` model with `TenantId`, `Name`, `Status`, `Metadata`, lifecycle timestamps
  - `TenantStatus` enum: Active, Inactive, Suspended, Archived, Deleted
  - `ITenantService` / `TenantService`: create, read, update, delete, activate, suspend, archive, search
  - `ITenantRepository` / `TenantRepository` backed by SQLite master database
- Database model
  - `TenantDatabase` with `FilePath`, `SchemaVersion`, `IsReadOnly`, `SizeBytes`
  - `ConnectionManager` for per-tenant `SqliteConnection` lifecycle
- Domain models: `Migration`, `Backup`, `TenantContext`, `TenantSettings`
- Exception types: `TenantNotFoundException`, `DatabaseAccessException`
- Dependency injection: `ServiceCollectionExtensions.AddSqliteMultiTenant`
- `MultiTenantOptions` configuration builder
- `TenantNameValidator` with slug rules and reserved-word rejection
- Structured `ILogger` integration throughout service layer
- `QueryBuilder` for parameterised SELECT / INSERT / UPDATE / DELETE
- MIT license, README, and initial CONTRIBUTING guide

---

## Semantic Versioning

- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible feature additions
- **PATCH** version for backwards-compatible bug fixes

## Version Support

| Version | Status    | .NET | Support Until |
|---------|-----------|------|---------------|
| 1.0.0   | Current   | 8.0+ | 2027-11-10   |
| 0.9.0   | Deprecated | 8.0+ | 2026-05-10  |

## Migration Guide

### From 0.9.0 to 1.0.0

No breaking changes. Update via NuGet:

```bash
dotnet add package SqliteMultiTenant --version 1.0.0
```

### From 0.8.x to 0.9.0

No breaking changes. Benchmark projects require BenchmarkDotNet 0.14.0+.

## Known Issues

- SQLite has a 30-second file lock timeout (see Troubleshooting in README)
- Network-attached storage degrades under high write concurrency
- Batch operations over 10 000 items may consume significant heap; increase `BatchSize` incrementally

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/Sarmkadan/sqlite-multi-tenant/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Sarmkadan/sqlite-multi-tenant/discussions)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Maintained by**: Vladyslav Zaiets ([https://sarmkadan.com](https://sarmkadan.com))
