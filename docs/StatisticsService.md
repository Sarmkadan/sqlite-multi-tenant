# StatisticsService

`StatisticsService` is the in-memory implementation of `IStatisticsService`. It records up to 10,000 recent system events and can summarize, aggregate, and analyze events within a requested time period. Access to the event collection is serialized, so the service can be shared between callers.

The class and all DTOs described below are in the `SqliteMultiTenant.Monitoring` namespace.

## Relationship to `IStatisticsService`

`StatisticsService` is declared as `public sealed class StatisticsService : IStatisticsService` and implements every member of that interface. Consumers should generally depend on `IStatisticsService`; the built-in dependency-injection registration maps the interface to `StatisticsService` as a singleton. See [IStatisticsService](IStatisticsService.md) for the interface-level documentation.

The signatures on this page reflect `src/Monitoring/StatisticsService.cs`, including the `TimeSpan period` and `string metricName` parameters.

## Public API

### Constructor

```csharp
public StatisticsService(ILogger<StatisticsService> logger)
```

Creates an empty statistics service using `logger` for diagnostic event-recording messages.

### `RecordEventAsync`

```csharp
public async Task RecordEventAsync(SystemEvent @event)
```

Records an event. The service replaces the supplied event's `Id` with a new GUID string and its `Timestamp` with the current UTC time. When the collection exceeds 10,000 events, the oldest events are discarded.

`event` must not be `null`; otherwise, the method throws `ArgumentNullException`.

### `GetStatisticsAsync`

```csharp
public async Task<SystemStatistics> GetStatisticsAsync(TimeSpan period)
```

Returns a summary of events whose timestamps are on or after `DateTime.UtcNow - period`. The result includes event counts, an event-type breakdown, the average duration in milliseconds, and the highest number of events recorded in a single second.

Only events with a non-null `Duration` contribute to `AverageResponseTime`. If the selected events contain no duration values, the current implementation throws `InvalidOperationException` while calculating the average.

### `GetMetricsAsync`

```csharp
public async Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period)
```

Selects events in the requested period whose `EventType` exactly equals `metricName`, groups them into UTC clock-hour buckets, and returns the buckets in timestamp order. Each bucket reports the average, minimum, maximum, and count of event `Value` entries.

`metricName` must not be `null` or empty; otherwise, the method throws `ArgumentNullException` or `ArgumentException`, respectively. An unmatched metric returns an empty list.

### `AnalyzeTrendAsync`

```csharp
public async Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period)
```

Selects matching events in timestamp order and analyzes their `Value` entries. Trend strength is the absolute slope of a simple linear regression over the ordered values; direction is `"Upward"`, `"Downward"`, or `"Stable"`. Volatility is the population standard deviation.

`metricName` must not be `null` or empty; otherwise, the method throws `ArgumentNullException` or `ArgumentException`, respectively. With fewer than two matching events, the method returns a `TrendAnalysis` whose `TrendDirection` is `"Insufficient data"`; its other fields retain their defaults.

## Data transfer objects

All DTO properties are publicly readable and writable.

### `SystemEvent`

Represents an observation submitted to the service.

| Field | Type | Description |
| --- | --- | --- |
| `Id` | `string` | Event identifier. Initialized to an empty string and replaced by `RecordEventAsync`. |
| `EventType` | `string` | Metric or event name used for filtering and breakdowns. Defaults to an empty string. |
| `Value` | `double` | Numeric observation used for metric aggregation and trend analysis. |
| `Duration` | `TimeSpan?` | Optional duration; statistics express its average in milliseconds. |
| `Timestamp` | `DateTime` | Event time. Replaced with the current UTC time when recorded. |
| `Tags` | `Dictionary<string, string>` | Arbitrary event metadata. Initialized to an empty dictionary and not used by the current calculations. |

### `SystemStatistics`

Summarizes events in a requested period.

| Field | Type | Description |
| --- | --- | --- |
| `Period` | `TimeSpan` | Requested lookback period. |
| `StartTime` | `DateTime` | UTC cutoff used to select events. |
| `EndTime` | `DateTime` | UTC time at which the result was completed. |
| `TotalEvents` | `int` | Number of selected events. |
| `EventTypeBreakdown` | `Dictionary<string, int>` | Selected event count keyed by exact event type. |
| `AverageResponseTime` | `double` | Mean duration, in milliseconds, of selected events that have a duration. |
| `PeakEventCount` | `int` | Largest number of selected events in any one-second bucket. |

### `AggregatedMetric`

Represents one hourly metric bucket.

| Field | Type | Description |
| --- | --- | --- |
| `Timestamp` | `DateTime` | Start of the hour containing the bucket's events. |
| `Value` | `double` | Average event value in the bucket. |
| `Count` | `int` | Number of events in the bucket. |
| `Min` | `double` | Minimum event value in the bucket. |
| `Max` | `double` | Maximum event value in the bucket. |

### `TrendAnalysis`

Describes value movement for one metric.

| Field | Type | Description |
| --- | --- | --- |
| `MetricName` | `string` | Analyzed event type. Defaults to an empty string. |
| `Period` | `TimeSpan` | Requested lookback period. |
| `DataPoints` | `int` | Number of matching events used in the analysis. |
| `AverageValue` | `double` | Mean of the matching event values. |
| `MinValue` | `double` | Minimum matching event value. |
| `MaxValue` | `double` | Maximum matching event value. |
| `TrendDirection` | `string` | `"Upward"`, `"Downward"`, `"Stable"`, or `"Insufficient data"`. Defaults to `"Stable"`. |
| `TrendStrength` | `double` | Absolute linear-regression slope across the ordered values. |
| `Volatility` | `double` | Population standard deviation of the values. |
| `Timestamp` | `DateTime` | UTC time at which a complete analysis was produced. |

## Usage example

The application registers the implementation as a singleton, allowing consumers to request the interface:

```csharp
services.AddSingleton<IStatisticsService, StatisticsService>();

public sealed class RequestMetrics
{
    private readonly IStatisticsService _statistics;

    public RequestMetrics(IStatisticsService statistics)
    {
        _statistics = statistics;
    }

    public async Task<TrendAnalysis> RecordAndAnalyzeAsync()
    {
        await _statistics.RecordEventAsync(new SystemEvent
        {
            EventType = "request.duration",
            Value = 125.4,
            Duration = TimeSpan.FromMilliseconds(125.4),
            Tags = new Dictionary<string, string>
            {
                ["tenant"] = "tenant-42"
            }
        });

        SystemStatistics summary =
            await _statistics.GetStatisticsAsync(TimeSpan.FromHours(24));

        List<AggregatedMetric> hourlyMetrics =
            await _statistics.GetMetricsAsync("request.duration", TimeSpan.FromHours(24));

        return await _statistics.AnalyzeTrendAsync(
            "request.duration",
            TimeSpan.FromHours(24));
    }
}
```

Because storage is in memory, recorded events are lost when the service instance is replaced or the process stops.
