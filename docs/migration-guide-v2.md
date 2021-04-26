## Migration Guide for v2.0

### Introduction
This guide provides a step-by-step process for migrating from v1.x to v2.0 of the SQLite Multi-Tenant library.

### Breaking Changes
- The `ITenantService` interface has been updated to include a new method `GetTenantAsync(string tenantId)`.
- The `IMigrationService` interface has been updated to include a new method `CreateMigrationAsync(string databaseId, string version, string name, string upScript, string downScript)`.

### New Features
- Support for async bulk import/export with streaming and progress reporting.
- Improved performance and reliability.

### Step-by-Step Migration
1. Update the NuGet package to v2.0.
2. Update the `ITenantService` and `IMigrationService` interfaces to include the new methods.
3. Update the database schema to include the new columns.
4. Run the migration script to update the database schema.
5. Update the application code to use the new features and methods.

### Code Examples
```csharp
// Example of using the new GetTenantAsync method
var tenantService = serviceProvider.GetRequiredService<ITenantService>();
var tenant = await tenantService.GetTenantAsync("tenant-id");

// Example of using the new CreateMigrationAsync method
var migrationService = serviceProvider.GetRequiredService<IMigrationService>();
var migration = await migrationService.CreateMigrationAsync("database-id", "001", "CreateTables", "CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);");
```

### Conclusion
The migration to v2.0 of the SQLite Multi-Tenant library is a significant update that includes new features and improvements. By following this guide, you can ensure a smooth transition to the new version.

### Additional Resources
- [Release Notes](https://github.com/Sarmkadan/sqlite-multi-tenant/releases)
- [API Documentation](https://github.com/Sarmkadan/sqlite-multi-tenant/blob/master/docs/api.md)
- [GitHub Issues](https://github.com/Sarmkadan/sqlite-multi-tenant/issues)