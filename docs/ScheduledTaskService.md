# ScheduledTaskService

`ScheduledTaskService` is the concrete in-memory scheduler in
`src/BackgroundWorkers/ScheduledTaskService.cs`. It implements both
`IScheduledTaskService` and `IHostedService`, executes asynchronous delegates on
an interval, optionally establishes a tenant context, and exposes execution
status for each registered task.

For the abstraction used by consumers, see [IScheduledTaskService](IScheduledTaskService.md).

## Construction

```csharp
public ScheduledTaskService(
    ILogger<ScheduledTaskService> logger,
    TenantContextHelper tenantContextHelper)
```

Both dependencies are required; the constructor throws `ArgumentNullException`
when either is `null`. The library's service registration adds
`TenantContextHelper` and `IScheduledTaskService` as singletons, with this class
as the interface implementation.

## Public methods

```csharp
public void RegisterTask(
    string taskId,
    Func<Task> taskAction,
    TimeSpan interval,
    string? tenantId = null)
```

Registers an enabled task under `taskId`. `taskId` must not be null, empty, or
whitespace, and `taskAction` must not be null. The first due time is
`DateTime.UtcNow + interval`; registration does not execute the delegate
immediately. Registering an existing ID replaces the stored task and resets its
tracking fields. If the service is already running, registration does not create
a polling loop for the new task; register tasks before calling `StartAsync`.

```csharp
public void UnregisterTask(string taskId)
```

Removes the task if present. Its polling cancellation token is cancelled and
disposed when one exists. An unknown ID is ignored.

```csharp
public Task StartAsync()
public Task StartAsync(CancellationToken cancellationToken)
```

Starts one polling loop for every task registered at that moment. Repeated calls
while running return without starting additional loops. The parameterless
overload uses `CancellationToken.None`; the token overload is also used by the
explicit `IHostedService.StartAsync(CancellationToken)` implementation.

```csharp
public Task StopAsync()
public Task StopAsync(CancellationToken cancellationToken)
```

Cancels and disposes all polling-loop tokens and marks the service as stopped.
The parameterless overload uses `CancellationToken.None`; the token overload is
also used by the explicit `IHostedService.StopAsync(CancellationToken)`
implementation. The method requests cancellation but does not await active task
delegates to finish.

```csharp
public Task<TaskExecutionStatus> GetTaskStatusAsync(string taskId)
```

Returns a snapshot containing the task ID, enabled state, last and next execution
times, execution and failure counts, tenant ID, and inherited operation-status
fields. It throws `KeyNotFoundException` when `taskId` is not registered.

## Task registration model

Tasks are held in an in-memory dictionary keyed by `taskId`; registrations and
status access are serialized with a semaphore. A registration creates a
`ScheduledTask` with these initial values:

| Field | Initial value |
| --- | --- |
| `Id`, `Action`, `Interval`, `TenantId` | Values supplied to `RegisterTask` |
| `NextExecutionAt` | Current UTC time plus `interval` |
| `LastExecutedAt`, `LastAttemptedAt`, `LastError` | `null` |
| `ExecutionCount`, `FailureCount` | `0` |
| `IsEnabled` | `true` |
| `IsExecuting` | `false` |

The model is process-local and is not persisted. There is no public API on the
service for pausing or changing an existing task; registering the same ID
replaces it. The `IsExecuting` check prevents overlapping executions of the same
task.

When `tenantId` is nonblank, the action is invoked through
`TenantContextHelper.ExecuteInTenantContext`; otherwise it runs without a tenant
context. The delegate itself receives no cancellation token, so stopping the
service cancels future polling rather than directly cancelling delegate work.

## Polling and backoff

Each task loop checks its task approximately once per second. When the current
UTC time is at or beyond `NextExecutionAt`, and the task is enabled and not
already executing, the service runs its action. Timing is fixed-delay: after an
attempt finishes or throws, the next due time is calculated from the current UTC
time rather than from the previous due time.

After a successful run, `ExecutionCount` is incremented, `LastExecutedAt` is set,
and `LastError` is cleared. After a failure, `FailureCount` is incremented and
`LastError` receives the exception message. The next interval is:

```text
no failures: interval
one or more failures: min(interval * 2^(min(FailureCount - 1, 5)), 24 hours)
```

Thus the first failure uses the base interval, subsequent failures use 2×, 4×,
8×, 16×, and then at most 32× the base interval, with an absolute maximum delay
of 24 hours. `FailureCount` is cumulative and is not reset after success, so once
a task has failed, later successful executions continue to use the backoff based
on its total failure count.

## Example

Register tasks before starting the service. In an ASP.NET Core application,
resolve the interface that is registered by the library:

```csharp
using SqliteMultiTenant.BackgroundWorkers;

var scheduler = app.Services.GetRequiredService<IScheduledTaskService>();

scheduler.RegisterTask(
    taskId: "purge-expired-sessions",
    taskAction: async () =>
    {
        await sessionStore.PurgeExpiredAsync();
    },
    interval: TimeSpan.FromMinutes(15),
    tenantId: "tenant-42");

await scheduler.StartAsync();

TaskExecutionStatus status =
    await scheduler.GetTaskStatusAsync("purge-expired-sessions");

// During application shutdown:
await scheduler.StopAsync();
```

When hosted lifecycle integration is used directly, the
`IHostedService.StartAsync(CancellationToken)` and
`IHostedService.StopAsync(CancellationToken)` calls route to the corresponding
token-aware public overloads.
