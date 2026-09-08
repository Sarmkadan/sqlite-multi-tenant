# StringBuilderPool

`StringBuilderPool` is a static, process-wide pool for reusing `StringBuilder` instances and reducing allocations. Access to the pool is synchronized, so `Rent` and `Return` can be called concurrently. A rented builder remains the caller's responsibility and must not be shared without separate synchronization.

## API

### `Rent(int capacity = 0)`

Rents a `StringBuilder` from the pool. If a pooled instance is available, the method removes it, increases its capacity when necessary, clears its contents, and returns it. If the pool is empty, it creates a new builder.

- `capacity`: The minimum requested capacity. The default value, `0`, uses the normal `StringBuilder` default when a new instance is created.
- Return value: A cleared `StringBuilder` ready for use.

The requested capacity is not a maximum. A reused builder may already have a capacity greater than requested, and `Rent` does not reduce it.

### `Return(StringBuilder)`

Clears a builder and makes it available for a later call to `Rent`, provided the pool has room. If the pool is already full, the builder is not retained. Passing `null` is ignored by the implementation.

After calling `Return`, the caller must stop using the builder because another caller may rent and modify the same instance.

## Pool limits

- The pool retains at most **64** `StringBuilder` instances (`MaxPooledInstances`).
- There is no configured maximum builder capacity. A large builder can be retained if the pool has room, and its allocated capacity is preserved when it is cleared.
- Capacity is increased with `EnsureCapacity` when a pooled builder is smaller than the positive capacity requested by `Rent`.

The limit applies only to idle builders stored in the pool. It does not limit the number of builders that can be rented at the same time.

## Usage

Always return the rented builder in a `finally` block so that exceptions do not prevent reuse:

```csharp
using SqliteMultiTenant.Utilities;

var builder = StringBuilderPool.Rent(256);
try
{
    builder.Append("Tenant: ");
    builder.Append(tenantId);

    return builder.ToString();
}
finally
{
    StringBuilderPool.Return(builder);
}
```

Create the string or otherwise consume the builder's contents before returning it. Do not retain a reference to the builder after the `finally` block.
