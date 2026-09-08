# BackupSchedulerService

Background service for scheduling automatic database backups. Runs on configurable intervals (default: daily at 2 AM UTC). Ensures all tenant databases are backed up for disaster recovery.

## Hosted Service Lifecycle

Inherits from `BackgroundService` which implements `IHostedService`. The service lifecycle is managed by the host:

- **Start**: When the application host starts, `StartAsync` is called (inherited from `BackgroundService`), which begins the background execution loop.
- **Execute**: The core logic runs in `ExecuteAsync(CancellationToken stoppingToken)` method.
- **Stop**: When the host shuts down, `StopAsync` is called (inherited), which triggers the cancellation token to gracefully stop the service.

### ExecuteAsync Method
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```
- Logs service startup with configured interval
- Calculates initial run time (daily at 2 AM UTC by default)
- Enters loop that continues until cancellation is requested:
  - Checks if current time has reached next scheduled run time
  - If so, executes backup job via `ExecuteBackupJobAsync`
  - Reschedules next run by adding the interval to current time
  - Sleeps for shorter of: time until next run or 1 minute
  - Handles exceptions by logging and delaying 5 minutes before continuing
  - Breaks loop on `OperationCanceledException` during shutdown

## Scheduling Interval

The backup interval is configurable via constructor parameter:

### Source
- **Constructor parameter**: `TimeSpan? interval = null`
- **Default value**: `TimeSpan.FromHours(24)` (24 hours / daily) when null is provided
- **Configuration origin**: In typical usage, this interval is sourced from `MultiTenantOptions.BackupInterval` (or similar configuration) and injected via dependency registration

### Usage in Scheduling Logic
1. Initial run time is set to 2 AM UTC of current day:
   ```csharp
   var nextRun = DateTime.UtcNow
       .AddHours(2)
       .RoundDownToMinute();
   ```
2. After each successful backup job, next run is calculated by:
   ```csharp
   nextRun = now.Add(_interval);
   ```
3. Service sleeps in intervals of 1 minute or less to check for run time, minimizing drift

## Public Methods

### Constructor
```csharp
public BackupSchedulerService(
    IBackupService backupService,
    ITenantService tenantService,
    ILogger<BackupSchedulerService> logger,
    TimeSpan? interval = null)
```
**Parameters**:
- `backupService`: Service for performing backup operations (required)
- `tenantService`: Service for retrieving tenant information (required)
- `logger`: Logger instance for this service (required)
- `interval`: Optional backup interval; defaults to 24 hours if null

**Exceptions**:
- `ArgumentNullException` if any required service parameter is null

### Protected Override Method (Hosted Service Lifecycle)
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```
Implements the background execution loop as described in [Hosted Service Lifecycle](#hosted-service-lifecycle).

### Private Method
```csharp
private async Task ExecuteBackupJobAsync(CancellationToken cancellationToken)
```
Executes backup for all active tenants:
- Retrieves all tenants via `_tenantService.GetAllTenantsAsync()`
- For each tenant, attempts to create a backup (current implementation logs but doesn't call actual backup service - placeholder for future implementation)
- Tracks success/failure counts and logs results
- Handles exceptions per tenant to prevent one failure from stopping others
- Respects cancellation token to allow graceful shutdown

## DI Registration

Register the service as a hosted service in the application's dependency injection container:

### Basic Registration
```csharp
services.AddHostedService<BackupSchedulerService>();
```
This uses the default interval of 24 hours.

### Configured Interval Registration
To configure the interval from application settings (e.g., `MultiTenantOptions.BackupInterval`):
```csharp
services.Configure<MultiTenantOptions>(configuration.GetSection("MultiTenant"));

services.AddHostedService<BackupSchedulerService>(provider =>
{
    var options = provider.GetRequiredService<IOptions<MultiTenantOptions>>();
    var backupService = provider.GetRequiredService<IBackupService>();
    var tenantService = provider.GetRequiredService<ITenantService>();
    var logger = provider.GetRequiredService<ILogger<BackupSchedulerService>>();
    
    // Assuming BackupInterval is in hours; adjust as needed
    var interval = TimeSpan.FromHours(options.BackupInterval);
    
    return new BackupSchedulerService(backupService, tenantService, logger, interval);
});
```
**Note**: The actual configuration path and property name (`MultiTenantOptions.BackupInterval`) should match your application's configuration structure.

## Key Implementation Details

- **Error Handling**: Individual tenant backup failures don't stop the overall backup job; service continues with next tenant
- **Cancellation Support**: Respects cancellation tokens for graceful shutdown during host termination
- **Logging**: Comprehensive logging at Information and Error levels for operational visibility
- **Thread Safety**: Designed to run as a singleton hosted service; state is confined to service instance