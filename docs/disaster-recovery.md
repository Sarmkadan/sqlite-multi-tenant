# Disaster Recovery Guide

This guide provides step-by-step runbooks for restoring tenant databases in production scenarios, handling schema version mismatches, and migrating databases across environments.

---

## Table of Contents

1. [Concepts and Terminology](#concepts-and-terminology)
2. [Runbook: Restore a Single Tenant from Backup](#runbook-restore-a-single-tenant-from-backup)
3. [Runbook: Full System Restore](#runbook-full-system-restore)
4. [Handling Schema Version Mismatches](#handling-schema-version-mismatches)
5. [Cross-Environment Migration (dev → staging → prod)](#cross-environment-migration)
6. [Verifying a Restored Database](#verifying-a-restored-database)
7. [Decision Tree for Incident Response](#decision-tree-for-incident-response)

---

## Concepts and Terminology

| Term | Meaning |
|---|---|
| **Source database** | The `.db` file being backed up or exported |
| **Backup file** | The copy produced by the backup API (identical SQLite format) |
| **Schema version** | Integer migration version recorded in the tenant's `Migrations` table |
| **WAL file** | Write-Ahead Log (`<db>-wal`); must be included when copying a live database |
| **Migration** | A numbered SQL script that upgrades (or downgrades) the schema |

---

## Runbook: Restore a Single Tenant from Backup

Use this procedure when a tenant reports data loss, corruption, or when you need to roll back a failed migration.

### Step 1 — Identify the correct backup

```csharp
// List available backups for the affected tenant database
var backupService = serviceProvider.GetRequiredService<IBackupService>();
var backups = await backupService.GetDatabaseBackupsAsync(databaseId);

// Choose the most recent completed and verified backup
var target = backups
    .Where(b => b.Status == BackupStatus.Verified)
    .OrderByDescending(b => b.CreatedAt)
    .First();

Console.WriteLine($"Restoring backup: {target.BackupId} created {target.CreatedAt:u}");
```

### Step 2 — Stop writes to the tenant database

Before restoring, prevent new writes to avoid conflicts:

```csharp
// Suspend the tenant so the application rejects new requests
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
await tenantService.SuspendTenantAsync(tenantId);
```

Also evict any pooled connections for that tenant:

```csharp
var connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();
await connectionManager.ClearTenantPoolAsync(tenantId);
```

### Step 3 — Copy the backup file over the live database

```bash
# On the server filesystem
TENANT_DB="/var/sqlite-multi-tenant/databases/tenant-42/tenant-42.db"
BACKUP_FILE="/var/sqlite-multi-tenant/backups/tenant-42_20240601_020000.db"

# Create a dated snapshot of the corrupt/current file before overwriting
cp "$TENANT_DB" "${TENANT_DB}.pre-restore.$(date +%s)"

# Replace with the backup
cp "$BACKUP_FILE" "$TENANT_DB"

# Remove any stale WAL and SHM files
rm -f "${TENANT_DB}-wal" "${TENANT_DB}-shm"
```

Alternatively, use the `BackupWithProgressAsync` API to do this from .NET:

```csharp
await backupService.BackupWithProgressAsync(
    sourceDatabasePath: target.BackupPath,   // backup → live
    destinationPath: tenant.DatabasePath,
    progress: new Progress<BackupProgress>(p =>
        Console.WriteLine($"Restore: {p.PercentComplete:F1}%")),
    cancellationToken: cts.Token);
```

### Step 4 — Verify the restored database

```bash
sqlite3 "$TENANT_DB" "PRAGMA integrity_check;"
# Expected output: ok
```

### Step 5 — Re-activate the tenant

```csharp
await tenantService.ActivateTenantAsync(tenantId);
```

---

## Runbook: Full System Restore

Use this procedure after catastrophic infrastructure failure when restoring the entire service.

```bash
# 1. Stop the application
sudo systemctl stop sqlite-multi-tenant

# 2. Clear all existing database files
rm -rf /var/sqlite-multi-tenant/databases/*

# 3. Restore from the latest backup archive
tar -xzf /backups/full-backup-latest.tar.gz -C /var/sqlite-multi-tenant/databases/

# 4. Verify each database
for db in /var/sqlite-multi-tenant/databases/**/*.db; do
  result=$(sqlite3 "$db" "PRAGMA integrity_check;" 2>&1)
  if [ "$result" != "ok" ]; then
    echo "CORRUPT: $db"
  else
    echo "OK:      $db"
  fi
done

# 5. Restart the application
sudo systemctl start sqlite-multi-tenant

# 6. Run the health check
curl -s http://localhost:5000/api/admin/health | jq .
```

---

## Handling Schema Version Mismatches

A mismatch occurs when the application's latest migration version is higher (or lower) than what is recorded in the restored database.

### Detecting a mismatch

```csharp
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
var applied = await migrationService.GetAppliedMigrationsAsync(tenantId);
var pending = await migrationService.GetPendingMigrationsAsync(tenantId);

Console.WriteLine($"Applied: {applied.Count}, Pending: {pending.Count}");
```

### Scenario A — Backup is behind the current schema (most common)

The restored database is at an older schema version. Apply pending migrations normally:

```csharp
foreach (var migration in pending.OrderBy(m => m.Version))
{
    try
    {
        await migrationService.ExecuteMigrationAsync(migration.MigrationId);
        Console.WriteLine($"Applied migration {migration.Version}: {migration.Name}");
    }
    catch (MigrationException ex)
    {
        Console.WriteLine($"Migration {migration.Version} failed: {ex.Message}");
        // Investigate before continuing — do not skip migrations blindly.
        break;
    }
}
```

### Scenario B — Backup is ahead of the current codebase

This can happen when restoring into an older application version (e.g., rolling back a deployment). Options:

1. **Deploy the matching application version** first, then restore the database.
2. **Apply the missing code** (upgrade the application) to match the database schema.
3. **Roll back the database schema** using the migration `Down` scripts (if available):

```csharp
// Roll back one version at a time until the versions align
var current = applied.Max(m => m.Version);
var target = expectedVersion;

while (current > target)
{
    var migration = applied.Single(m => m.Version == current);
    await migrationService.RollbackMigrationAsync(migration.MigrationId);
    current--;
}
```

> **Warning:** Rolling back a schema may cause data loss if the `Down` script drops columns or tables. Always create a point-in-time backup before rolling back.

---

## Cross-Environment Migration

This section describes how to move a tenant database from one environment to another (e.g., cloning production data into staging for debugging).

### Prerequisites

- Both environments run the **same application version**.
- Both environments have compatible SQLite builds.
- The target environment has **sufficient disk space**.

### Step 1 — Create a consistent backup from the source environment

```bash
# On the source server (production)
sqlite3 /var/sqlite-multi-tenant/databases/tenant-42/tenant-42.db \
  ".backup /tmp/tenant-42-export.db"
```

Or via the .NET API:

```csharp
await backupService.BackupWithProgressAsync(
    sourceDatabasePath: "/var/sqlite-multi-tenant/databases/tenant-42/tenant-42.db",
    destinationPath: "/tmp/tenant-42-export.db",
    progress: new Progress<BackupProgress>(p =>
        Console.WriteLine($"{p.PercentComplete:F1}% ({p.PagesCopied}/{p.TotalPages} pages)")));
```

### Step 2 — Transfer the file securely

```bash
# Copy to the target environment using scp or a secure object store
scp /tmp/tenant-42-export.db staging-server:/tmp/tenant-42-import.db
```

### Step 3 — Register the tenant on the target environment

If the tenant does not yet exist in the target environment, provision a placeholder first:

```csharp
var provisioner = serviceProvider.GetRequiredService<TenantProvisioner>();
// Provision with a temporary empty database; we will overwrite it next.
await provisioner.ProvisionTenantAsync("tenant-42", "Tenant 42");
```

### Step 4 — Replace the empty database with the exported file

```bash
TARGET_DB="/var/sqlite-multi-tenant/databases/tenant-42/tenant-42.db"
cp /tmp/tenant-42-import.db "$TARGET_DB"
rm -f "${TARGET_DB}-wal" "${TARGET_DB}-shm"
```

### Step 5 — Align schema versions

Run any pending migrations if the target environment is on a newer application version:

```csharp
var pending = await migrationService.GetPendingMigrationsAsync("tenant-42");
foreach (var m in pending.OrderBy(x => x.Version))
    await migrationService.ExecuteMigrationAsync(m.MigrationId);
```

### Step 6 — Scrub sensitive production data (optional but recommended)

When copying production data to non-production environments, remove or anonymise PII:

```sql
-- Example: anonymise contact emails in the staging database
UPDATE Tenants SET ContactEmail = 'anonymised-' || TenantId || '@example.com';
```

---

## Verifying a Restored Database

Run these checks after any restore operation to confirm the database is healthy before re-enabling tenant access.

```bash
# 1. SQLite integrity check
sqlite3 "$TENANT_DB" "PRAGMA integrity_check;"

# 2. Check for WAL journal mode remnants
sqlite3 "$TENANT_DB" "PRAGMA journal_mode;"

# 3. Count records in core tables
sqlite3 "$TENANT_DB" "SELECT COUNT(*) FROM Tenants;"
sqlite3 "$TENANT_DB" "SELECT COUNT(*) FROM AuditLog;"

# 4. Check the applied migration version
sqlite3 "$TENANT_DB" "SELECT MAX(Version) FROM Migrations;"
```

From .NET:

```csharp
var storageInfo = await tenantService.GetTenantDatabaseSizeAsync(tenantId);
Console.WriteLine($"Database size: {storageInfo.SizeBytes / 1024.0 / 1024.0:F2} MB");
Console.WriteLine($"WAL size: {storageInfo.WalSizeBytes / 1024.0:F1} KB");
```

---

## Decision Tree for Incident Response

```
Tenant reports data problem
        │
        ▼
  Is the database file missing?
  ├── Yes → Step 3 of "Restore Single Tenant" runbook
  └── No
        │
        ▼
  sqlite3 PRAGMA integrity_check returns error?
  ├── Yes → Restore from backup (full "Restore Single Tenant" runbook)
  └── No
        │
        ▼
  Is the schema version mismatched?
  ├── Yes → Follow "Schema Version Mismatch" section
  └── No
        │
        ▼
  Are pending migrations present?
  ├── Yes → Apply pending migrations (Scenario A)
  └── No → Investigate at application level (check logs, audit trail)
```

---

## See Also

- [Deployment Guide](deployment.md) — production infrastructure setup
- [Migration Guide v2](migration-guide-v2.md) — upgrade procedures
- [Getting Started](getting-started.md) — initial setup
