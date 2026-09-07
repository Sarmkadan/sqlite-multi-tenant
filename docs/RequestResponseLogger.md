# RequestResponseLogger

## Overview

`RequestResponseLogger` is the in-memory implementation of `IRequestResponseLogger`. It records HTTP request and response DTOs, supports filtered retrieval, and calculates aggregate logging statistics. The service is registered as an `IRequestResponseLogger` singleton, so its stored entries are process-local and are lost when the process stops.

The implementation serializes access to both collections with a `SemaphoreSlim`.

## Interface relationship

`RequestResponseLogger` implements the four methods declared by `IRequestResponseLogger`:

- `LogRequestAsync`
- `LogResponseAsync`
- `GetRequestLogsAsync`
- `GetResponseLogsAsync`

`GetStatisticsAsync` is a public method on the concrete class, but it is not part of `IRequestResponseLogger`. Code resolving the service through the interface cannot call that method without accessing the concrete implementation.

See [IRequestResponseLogger.md](IRequestResponseLogger.md) for the interface documentation.

## Public API

```csharp
public RequestResponseLogger(ILogger<RequestResponseLogger> logger)
public Task LogRequestAsync(RequestLog request)
public Task LogResponseAsync(ResponseLog response)
public Task<List<RequestLog>> GetRequestLogsAsync(LogFilter filter)
public Task<List<ResponseLog>> GetResponseLogsAsync(LogFilter filter)
public Task<LoggingStatistics> GetStatisticsAsync()
```

### `RequestResponseLogger`

Creates an empty in-memory logger and uses the supplied `ILogger<RequestResponseLogger>` for debug messages.

### `LogRequestAsync`

Applies request sampling before storing the entry. For a sampled request, the method replaces `Id` with a new GUID string, replaces `Timestamp` with `DateTime.UtcNow`, appends the same `RequestLog` instance to the request collection, and trims the oldest entries when necessary. A `null` argument throws `ArgumentNullException`.

### `LogResponseAsync`

Stores every response; response logging does not use the request sampling rule. The method replaces `Id` with a new GUID string, replaces `Timestamp` with `DateTime.UtcNow`, appends the same `ResponseLog` instance to the response collection, and trims the oldest entries when necessary. A `null` argument throws `ArgumentNullException`.

### `GetRequestLogsAsync`

Filters request entries by exact, case-sensitive `Method`; case-insensitive substring matching on `Path`; inclusive `StartTime`; and inclusive `EndTime`. Results are ordered by descending timestamp and restricted with `LogFilter.Limit`. A `null` filter throws `ArgumentNullException`.

### `GetResponseLogsAsync`

Filters response entries by exact `StatusCode`, inclusive `StartTime`, inclusive `EndTime`, and inclusive minimum duration (`DurationMs >= MinDuration`). Results are ordered by descending timestamp and restricted with `LogFilter.Limit`. A `null` filter throws `ArgumentNullException`.

### `GetStatisticsAsync`

Returns statistics for the entries currently retained in memory:

- request and response collection counts;
- average request body length, treating a `null` body as length zero;
- average response duration in milliseconds;
- most common request path and method.

Empty collections produce zero averages and `"N/A"` for the most common path and method. Because request sampling and collection trimming occur before these statistics are calculated, the totals do not represent all application traffic.

## In-memory limits and sampling

| Setting | Value | Behavior |
| --- | ---: | --- |
| `MaxLogsInMemory` | `5000` | Applies independently to request and response collections. After an append takes a collection over the limit, its oldest entries are removed. At most 5,000 requests and 5,000 responses are retained. |
| `SamplingRate` | `100` | Applies only to requests. A random value in the range 0 through 99 is generated for each request, and only value 0 is stored: approximately one request out of every 100. |

Both values are private constants and cannot be configured through the public API.

## DTOs

### `RequestLog`

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string` | Empty string | Replaced with a new GUID string when the request is stored. |
| `Timestamp` | `DateTime` | Default value | Replaced with the current UTC time when the request is stored. |
| `Method` | `string` | Empty string | HTTP method. |
| `Path` | `string` | Empty string | Request path. |
| `Host` | `string` | Empty string | Request host. |
| `Body` | `string?` | `null` | Optional request body. |
| `Headers` | `Dictionary<string, string>` | Empty dictionary | Request headers. |
| `QueryParameters` | `Dictionary<string, string>` | Empty dictionary | Query-string parameters. |
| `IpAddress` | `string` | Empty string | Client IP address. |

### `ResponseLog`

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string` | Empty string | Replaced with a new GUID string when the response is stored. |
| `Timestamp` | `DateTime` | Default value | Replaced with the current UTC time when the response is stored. |
| `StatusCode` | `int` | `0` | HTTP status code. |
| `DurationMs` | `long` | `0` | Response duration in milliseconds. |
| `Body` | `string?` | `null` | Optional response body. |
| `ResponseSize` | `long` | `0` | Response size as supplied by the caller. |
| `Headers` | `Dictionary<string, string>` | Empty dictionary | Response headers. |

### `LogFilter`

The same filter DTO is used for request and response queries. Properties that do not apply to the selected query are ignored.

| Property | Type | Default | Used for |
| --- | --- | --- | --- |
| `Method` | `string?` | `null` | Request method equality. Empty strings do not filter. |
| `Path` | `string?` | `null` | Request path substring. Empty strings do not filter. |
| `StatusCode` | `int?` | `null` | Response status-code equality. |
| `StartTime` | `DateTime?` | `null` | Inclusive lower timestamp bound for both entry types. |
| `EndTime` | `DateTime?` | `null` | Inclusive upper timestamp bound for both entry types. |
| `MinDuration` | `long?` | `null` | Inclusive response-duration lower bound. |
| `Limit` | `int` | `100` | Maximum number of results returned after sorting. |

### `LoggingStatistics`

| Property | Type | Description |
| --- | --- | --- |
| `TotalRequestsLogged` | `int` | Number of request entries currently retained. |
| `TotalResponsesLogged` | `int` | Number of response entries currently retained. |
| `AverageRequestSize` | `double` | Average retained request body length in characters. |
| `AverageResponseTime` | `double` | Average retained response duration in milliseconds. |
| `MostCommonPath` | `string` | Most frequent retained request path, or `"N/A"`. |
| `MostCommonMethod` | `string` | Most frequent retained request method, or `"N/A"`. |
