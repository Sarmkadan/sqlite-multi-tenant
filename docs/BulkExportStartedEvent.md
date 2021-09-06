# BulkExportStartedEvent

A domain event that signals the start of a bulk export operation for a specific tenant database. It is published by the export service before any data is read or written, allowing subscribers to perform logging, monitoring, or preparatory work.

## API

### DatabaseId
**Type:** `string`  
**Purpose:** Holds the unique identifier of the database that is being exported.  
**Remarks:** Set by the publisher after constructing the event; no validation is performed in the constructor.

### TableNames
**Type:** `IReadOnlyList<string>`  
**Purpose:** Contains the names of the tables that will be included in the export operation.  
**Remarks:** The list is exposed as read‑only to prevent accidental modification by subscribers, but the underlying list can be mutated by the publisher before the event is published.

### Format
**Type:** `string`  
**Purpose:** Specifies the output format of the exported data (e.g., `"CSV"`, `"JSON"`).  
**Remarks:** Expected to match one of the supported format strings defined elsewhere in the codebase; the event does not enforce this.

### OperationId
**Type:** `string`  
**Purpose:** Provides a correlation identifier that links the start, progress, and completion (or failure) of a bulk export operation.  
**Remarks:** Should be unique per operation; publishers typically generate a GUID.

### BulkExportStartedEvent()
**Signature:** `public BulkExportStartedEvent() : base(nameof(BulkExportStartedEvent))`  
**Purpose:** Parameterless constructor that invokes the base `DomainEvent` constructor with the event's type name.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:** None are thrown directly; any exceptions would originate from the base class construction, which does not throw under normal circumstances.

## Usage

### Creating and publishing the event
```csharp
var exportStarted = new BulkExportStartedEvent
{
    DatabaseId = "tenant-42-db",
    TableNames = new List<string> { "Customers", "Orders" }.AsReadOnly(),
    Format = "CSV",
    OperationId = Guid.NewGuid().ToString()
};

_eventPublisher.Publish(exportStarted);
```

### Handling the event in a subscriber
```csharp
public class ExportStartedLogger : IHandle<BulkExportStartedEvent>
{
    private readonly ILogger<ExportStartedLogger> _logger;

    public ExportStartedLogger(ILogger<ExportStartedLogger> logger) =>
        _logger = logger;

    public Task Handle(BulkExportStartedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Bulk export started for DatabaseId={DatabaseId}, OperationId={OperationId}, Format={Format}, Tables={TableCount}",
            @event.DatabaseId,
            @event.OperationId,
            @event.Format,
            @event.TableNames.Count);

        return Task.CompletedTask;
    }
}
```

## Notes
- The event class does not enforce immutability; all members are mutable fields. Publishers should treat the instance as effectively immutable after publication to avoid race conditions.
- Concurrent reads of the fields after publishing are safe only if no thread mutates the instance thereafter. Mutating fields after the event has been handed to subscribers can lead to inconsistent observations.
- Null or empty values for `DatabaseId`, `Format`, or `OperationId` are not prevented by the constructor; consumers should validate these values if required by their logic.
- The `TableNames` list is exposed as `IReadOnlyList<string>` to discourage modification, but the underlying list remains mutable; publishers must ensure the list is not altered after the event is published.
