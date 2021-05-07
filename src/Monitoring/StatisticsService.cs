#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Monitoring;

/// <summary>
/// Collects and analyzes system statistics and usage metrics.
/// Provides insights into system health, performance, and resource utilization.
/// Supports time-series data aggregation and trend analysis.
/// </summary>
public interface IStatisticsService
{
    Task RecordEventAsync(SystemEvent @event);
    Task<SystemStatistics> GetStatisticsAsync(TimeSpan period);
    Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period);
    Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period);
}

public sealed class StatisticsService : IStatisticsService {
    private readonly List<SystemEvent> _events;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<StatisticsService> _logger;
    private const int MaxEventsInMemory = 10000;

    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger;
        _events = new List<SystemEvent>();
        _semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Records a system event for statistics.
    /// </summary>
    public async Task RecordEventAsync(SystemEvent @event)
    {
        try
        {
            await _semaphore.WaitAsync();

            @event.Id = Guid.NewGuid().ToString();
            @event.Timestamp = DateTime.UtcNow;

            _events.Add(@event);

            // Maintain memory limit
            if (_events.Count > MaxEventsInMemory)
                _events.RemoveRange(0, _events.Count - MaxEventsInMemory);

            _logger.LogDebug($"Event recorded: {(@event.EventType)}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets statistics for a given time period.
    /// </summary>
    public async Task<SystemStatistics> GetStatisticsAsync(TimeSpan period)
    {
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events.Where(e => e.Timestamp >= cutoffTime).ToList();

            var stats = new SystemStatistics
            {
                Period = period,
                StartTime = cutoffTime,
                EndTime = DateTime.UtcNow,
                TotalEvents = relevantEvents.Count,
                EventTypeBreakdown = relevantEvents
                    .GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageResponseTime = relevantEvents
                    .Where(e => e.Duration.HasValue)
                    .Average(e => e.Duration!.Value.TotalMilliseconds),
                PeakEventCount = GetPeakEventCount(relevantEvents)
            };

            return stats;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets aggregated metrics for a specific metric type.
    /// </summary>
    public async Task<List<AggregatedMetric>> GetMetricsAsync(string metricName, TimeSpan period)
    {
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events
                .Where(e => e.Timestamp >= cutoffTime && e.EventType == metricName)
                .ToList();

            // Aggregate by hour
            var aggregated = relevantEvents
                .GroupBy(e => e.Timestamp.AddSeconds(-(e.Timestamp.Second + e.Timestamp.Minute * 60)))
                .Select(g => new AggregatedMetric
                {
                    Timestamp = g.Key,
                    Value = g.Average(e => e.Value),
                    Count = g.Count(),
                    Min = g.Min(e => e.Value),
                    Max = g.Max(e => e.Value)
                })
                .OrderBy(m => m.Timestamp)
                .ToList();

            return aggregated;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Analyzes trends in a metric over time.
    /// </summary>
    public async Task<TrendAnalysis> AnalyzeTrendAsync(string metricName, TimeSpan period)
    {
        try
        {
            await _semaphore.WaitAsync();

            var cutoffTime = DateTime.UtcNow - period;
            var relevantEvents = _events
                .Where(e => e.Timestamp >= cutoffTime && e.EventType == metricName)
                .OrderBy(e => e.Timestamp)
                .ToList();

            if (relevantEvents.Count < 2)
                return new TrendAnalysis { TrendDirection = "Insufficient data" };

            var values = relevantEvents.Select(e => e.Value).ToList();

            // Calculate trend
            var trend = CalculateTrend(values);
            var volatility = CalculateVolatility(values);

            return new TrendAnalysis
            {
                MetricName = metricName,
                Period = period,
                DataPoints = relevantEvents.Count,
                AverageValue = values.Average(),
                MinValue = values.Min(),
                MaxValue = values.Max(),
                TrendDirection = trend > 0 ? "Upward" : trend < 0 ? "Downward" : "Stable",
                TrendStrength = Math.Abs(trend),
                Volatility = volatility,
                Timestamp = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private int GetPeakEventCount(List<SystemEvent> events)
    {
        if (events.Count == 0)
            return 0;

        return events
            .GroupBy(e => e.Timestamp.AddSeconds(-(e.Timestamp.Second)))
            .Max(g => g.Count());
    }

    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        // Simple linear regression trend
        int n = values.Count;
        double sumX = Enumerable.Range(0, n).Sum(i => (double)i);
        double sumY = values.Sum();
        double sumXY = Enumerable.Range(0, n).Sum(i => i * values[i]);
        double sumX2 = Enumerable.Range(0, n).Sum(i => i * i);

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    private double CalculateVolatility(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        double mean = values.Average();
        double variance = values.Average(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(variance);
    }
}

public sealed class SystemEvent {
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public double Value { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public sealed class SystemStatistics {
    public TimeSpan Period { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public int PeakEventCount { get; set; }
}

public sealed class AggregatedMetric {
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

public sealed class TrendAnalysis {
    public string MetricName { get; set; } = string.Empty;
    public TimeSpan Period { get; set; }
    public int DataPoints { get; set; }
    public double AverageValue { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public string TrendDirection { get; set; } = "Stable";
    public double TrendStrength { get; set; }
    public double Volatility { get; set; }
    public DateTime Timestamp { get; set; }
}
