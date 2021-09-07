# DomainEvent
Base class for all domain events in the **sqlite-multi-tenant** project. It provides common metadata—an identifier, timestamp, event type, and optional tenant identifier—that enables uniform handling, storage, and routing of events across the multi‑tenant system.

## API
### DomainEvent fields
| Member | Type | Purpose | Parameters | Return value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `EventId` | `string` | Unique identifier for the event occurrence. Typically a GUID string. | – | – | None |
| `OccurredAt` | `DateTime` | Timestamp indicating when the event happened (usually UTC). | – | – | None |
| `EventType` | `string` | Discriminator that identifies the concrete event subclass (set by the subclass constructor via `nameof`). Enables polymorphic handling without reflection. | – | – | None |
| `TenantId` | `string?` | Optional identifier of the tenant associated with the event. `null` denotes a system‑wide event not tied to any specific tenant. | – | – | None |

### TenantCreatedEvent (`sealed class : DomainEvent`)
| Member | Type | Purpose | Parameters | Return value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `TenantId` | `string` | Identifier of the newly created tenant. | – | – | None |
| `TenantName` | `string` | Human‑readable name of the tenant. | – | – | None |
| `ContactEmail` | `string` | Email address for tenant contact. | – | – | None |
| Constructor | `TenantCreatedEvent()` | Initializes the event, setting `EventType` to `"TenantCreatedEvent"` via `base(nameof(TenantCreatedEvent))`. | – | – | None |

### TenantUpdatedEvent (`sealed class : DomainEvent`)
| Member | Type | Purpose | Parameters | Return value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `TenantId` | `string` | Identifier of the tenant being updated. | – | – | None |
| `OldName` | `string` | Tenant name before the update. | – | – | None |
| `NewName` | `string` | Tenant name after the update. | – | – | None |
| Constructor | `TenantUpdatedEvent()` | Initializes the event, setting `EventType` to `"TenantUpdatedEvent"` via `base(nameof(TenantUpdatedEvent))`. | – | – | None |

### TenantSuspendedEvent (`sealed class : DomainEvent`)
| Member | Type | Purpose | Parameters | Return value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `TenantId` | `string` | Identifier of the tenant being suspended. | – | – | None |
| `SuspendedBy` | `string` | Identifier of the user or process that initiated the suspension. | – | – | None |
| `Reason` | `string` | Explanation for the suspension. | – | – | None |
| Constructor | `TenantSuspendedEvent()` | Initializes the event, setting `EventType` to `"TenantSuspendedEvent"` via `base(nameof(TenantSuspendedEvent))`. | – | – | None |

### BackupStartedEvent (`sealed class : DomainEvent`)
| Member | Type | Purpose | Parameters | Return value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| (no additional fields) | – | Represents the start of a backup operation; inherits all base members. | – | – | None |
| Constructor | `BackupStartedEvent()` | Initializes the event, setting `EventType` to `"BackupStartedEvent"` via `base(nameof(BackupStartedEvent))`. | – | – | None |

## Usage
### Creating a tenant‑creation event
```csharp
var @event = new TenantCreatedEvent
{
    EventId      = Guid.NewGuid().ToString(),
    OccurredAt   = DateTime.UtcNow,
    TenantId     = "tenant-123",
    TenantName   = "Acme Corp",
    ContactEmail = "admin@acme.example"
};

// Publish or store @event as needed
```

### Handling events polymorphously
```csharp
void Handle(DomainEvent @event)
{
    switch (@event)
    {
        case TenantCreatedEvent created:
            // provision resources for the new tenant
            ProvisionTenant(created.TenantId, created.TenantName, created.ContactEmail);
            break;

        case TenantUpdatedEvent updated:
            // rename tenant in read‑model
            RenameTenant(updated.TenantId, updated.OldName, updated.NewName);
            break;

        case TenantSuspendedEvent suspended:
            // disable tenant access
            SuspendTenant(suspended.TenantId, suspended.SuspendedBy, suspended.Reason);
            break;

        case BackupStartedEvent _:
            // trigger backup monitoring
            StartBackupMonitoring();
            break;

        default:
            // unknown event type – log for investigation
            Logger.Warn($"Unhandled event type: {@event.EventType}");
            break;
    }
}
```

## Notes
- All members are **mutable public fields**; concurrent reads and writes without external synchronization can lead to torn or inconsistent state. In a multi‑threaded context, treat the object as immutable after initialization or protect access with locks/`Interlocked` operations where appropriate.
- `EventId` should be assigned a globally unique value before the event is published; the base class does not generate it automatically.
- `OccurredAt` is expected to be set to `DateTime.UtcNow` (or another reliable clock) to maintain chronological ordering across tenants.
- `EventType` is set once by the subclass constructor and should not be altered afterward; changing it would break polymorphic handling based on this field.
- `TenantId` may be `null`. Consumers must check for null when the event’s semantics depend on a specific tenant (e.g., routing to a tenant‑specific store).
- The sealed event subclasses currently declare no additional behavior beyond data storage; future versions may add methods or validation, but the current contract is limited to the fields shown above.
