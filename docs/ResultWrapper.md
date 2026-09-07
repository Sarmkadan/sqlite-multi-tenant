# Result wrappers

`src/Api/Responses/ResultWrapper.cs` defines response models in the
`SqliteMultiTenant.Api.Responses` namespace. They provide consistent shapes for
single-value responses, paginated responses, operations without return data,
and batch operations.

## `Result<T>`

`Result<T>` wraps a single value of type `T`.

| Member | Type | Description |
| --- | --- | --- |
| `Success` | `bool` | Indicates whether the request succeeded. |
| `Data` | `T?` | The returned value. It is unset by the failure factories. |
| `Errors` | `List<string>` | Error messages; initialized to an empty list. |
| `Message` | `string?` | An optional success or informational message. |
| `Metadata` | `ResultMetadata?` | Optional response metadata. Factories do not populate it. |

Use `Result<T>.Ok(data, message)` to create a successful result. It sets
`Success` to `true`, assigns `Data`, and optionally assigns `Message`.

Use `Result<T>.Fail(error)` for one error or `Result<T>.Fail(errors)` for a
`List<string>`. Both set `Success` to `false` and populate `Errors`; they do not
set `Data`, `Message`, or `Metadata`.

```csharp
var success = Result<Customer>.Ok(customer, "Customer loaded");
var failure = Result<Customer>.Fail("Customer was not found");
```

All properties remain publicly settable, so callers may attach metadata or
otherwise customize a factory-created result. `ToString()` returns a diagnostic
representation of all five properties.

## `PaginatedResult<T>`

`PaginatedResult<T>` represents a page of API response items.

| Member | Type | Description |
| --- | --- | --- |
| `Success` | `bool` | Indicates whether retrieving the page succeeded. |
| `Items` | `List<T>` | Items in the current page; initialized to an empty list. |
| `Pagination` | `PaginationMetadata` | Page number, size, counts, and navigation state. |
| `Errors` | `List<string>` | Error messages; initialized to an empty list. |
| `Message` | `string?` | An optional message. |

`PaginatedResult<T>.Ok(items, pageNumber, pageSize, totalCount)` sets the result
as successful, assigns the items, and builds `Pagination`. `TotalPages` is
calculated as `Math.Ceiling((double)totalCount / pageSize)` and converted to an
`int`. Callers should therefore provide a valid, nonzero page size.

`PaginatedResult<T>.Fail(error)` sets `Success` to `false` and creates a
single-entry error list. Items and pagination retain their default empty/zero
values.

```csharp
var page = PaginatedResult<Customer>.Ok(
    customers,
    pageNumber: 2,
    pageSize: 25,
    totalCount: 61);
```

## Metadata

### `ResultMetadata`

`ResultMetadata` contains optional context for a `Result<T>`:

- `Timestamp` is initialized to `DateTime.UtcNow` when the object is created.
- `TraceId` optionally associates the response with a trace.
- `StatusCode` optionally records a status code.
- `AdditionalData` is an initially empty `Dictionary<string, object>` for
  application-specific values.

### `PaginationMetadata`

`PaginationMetadata` contains `PageNumber`, `PageSize`, `TotalCount`, and
`TotalPages`. It also derives two read-only navigation flags:

- `HasPreviousPage` is `true` when `PageNumber > 1`.
- `HasNextPage` is `true` when `PageNumber < TotalPages`.

These flags reflect the stored values only; the class does not validate page
numbers or counts.

## `OperationResult`

`OperationResult` describes an action that does not return a data value. It has
`Success`, optional `Message`, an initialized `Errors` list, and a `Timestamp`
initialized to `DateTime.UtcNow`.

- `OperationResult.Ok(message)` sets `Success` to `true` and optionally sets the
  message.
- `OperationResult.Fail(error)` creates a failed result with one error.
- `OperationResult.Fail(errors)` creates a failed result using the supplied
  `List<string>`.

```csharp
var completed = OperationResult.Ok("Customer deleted");
var rejected = OperationResult.Fail(new List<string>
{
    "Customer is active",
    "Active customers cannot be deleted"
});
```

## Batch results

### `BatchOperationResult`

`BatchOperationResult` summarizes an operation across multiple items:

| Member | Type | Description |
| --- | --- | --- |
| `SuccessCount` | `int` | Number of successful items. |
| `FailureCount` | `int` | Number of failed items. |
| `Items` | `List<BatchItemResult>` | Per-item results; initialized to an empty list. |
| `Success` | `bool` | Read-only; `true` exactly when `FailureCount == 0`. |

`GetTotalCount()` returns `SuccessCount + FailureCount`. It uses the summary
counts and does not calculate the value from `Items.Count`.

### `BatchItemResult`

Each `BatchItemResult` contains:

- `ItemId`, initialized to an empty string;
- `Success`, indicating the outcome for that item;
- optional `Error` text; and
- optional `Data` as `object`, for item-specific output.

The model does not enforce a relationship between `Success` and `Error` or
`Data`; the producer is responsible for setting a consistent combination.

## A second `PaginatedResult<T>`

`src/Repositories/GenericRepository.cs` also declares a type named
`PaginatedResult<T>`, but in the `SqliteMultiTenant.Repositories` namespace. It
is not the API response type documented above.

The repository version constrains `T` to a reference type (`where T : class`)
and exposes pagination directly through `Items`, `TotalCount`, `PageNumber`,
`PageSize`, `TotalPages`, `HasPreviousPage`, and `HasNextPage`. It has no
`Success`, `Errors`, `Message`, `Pagination`, or factory methods. When both
namespaces are in scope, use a namespace alias or a fully qualified type name to
make the intended shape clear.

```csharp
using ApiPage = SqliteMultiTenant.Api.Responses.PaginatedResult<Customer>;
using RepositoryPage = SqliteMultiTenant.Repositories.PaginatedResult<Customer>;
```
