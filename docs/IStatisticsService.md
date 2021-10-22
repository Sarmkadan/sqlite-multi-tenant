# IStatisticsService

The `IStatisticsService` interface and its implementing `StatisticsService` class provide facilities to record, aggregate, and analyze system events within a multi-tenant SQLite environment. It is designed to track event types, durations, and metadata over configurable time periods, enabling trend analysis and system health monitoring.

## API

### `StatisticsService` (public sealed class)

#### `public StatisticsService()`
Initializes a new instance of the `StatisticsService` class. No external dependencies are required for construction.

#### `public async Task RecordEventAsync(SystemEvent systemEvent)`
Records a system event for later aggregation and analysis.

- **Parameters**
  - `systemEvent`: The event to record. Must not be `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `systemEvent` is `null`.

#### `public async Task<SystemStatistics> GetStatisticsAsync()`
Retrieves aggregated statistics for all recorded events.

- **Returns**
  - A `SystemStatistics` object representing the aggregated data over the service's configured period.
- **Exceptions**
  - May throw `InvalidOperationException` if the service has not been properly initialized or if no events have been recorded.

#### `public async Task<List<AggregatedMetric>> GetMetricsAsync()`
Retrieves a list of aggregated metrics derived from recorded events.

- **Returns**
  - A list of `AggregatedMetric` objects, each representing a computed metric over the recorded data.
- **Exceptions**
  - May throw `InvalidOperationException` if the service has not been properly initialized or if no events have been recorded.

#### `public async Task<TrendAnalysis> AnalyzeTrendAsync()`
Performs trend analysis on the recorded events to identify patterns or anomalies.

- **Returns**
  - A `TrendAnalysis` object containing trend insights and potential anomalies.
- **Exceptions**
  - May throw `InvalidOperationException` if the service has not been properly initialized or if no events have been recorded.

---

### `SystemEvent` (public sealed class)

#### `public string Id`
Gets or sets a unique identifier for the event. Must not be `null` or empty.

#### `public string EventType`
Gets or sets the type of the event. Must not be `null` or empty.

#### `public double Value`
Gets or sets a numeric value associated with the event.

#### `public TimeSpan? Duration`
Gets or sets the duration of the event, if applicable. May be `null`.

#### `public DateTime Timestamp`
Gets or sets the time at which the event occurred.

#### `public Dictionary<string, string> Tags`
Gets or sets a collection of key-value pairs representing metadata for the event.

---

### `SystemStatistics` (public sealed class)

#### `public TimeSpan Period`
Gets the time period over which statistics were collected.

#### `public DateTime StartTime`
Gets the start time of the statistics collection period.

#### `public DateTime EndTime`
Gets the end time of the statistics collection period.

#### `public int TotalEvents`
Gets the total number of events recorded during the period.

#### `public Dictionary<string, int> EventTypeBreakdown`
Gets a breakdown of event counts by event type.

#### `public double AverageResponseTime`
Gets the average response time across all events with a duration.

## Usage

### Recording and retrieving statistics
