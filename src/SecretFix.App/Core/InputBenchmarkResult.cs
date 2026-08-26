namespace SecretFix.Core;

/// <summary>Local Raw Input measurements. Event cadence is not a firmware USB polling-rate claim.</summary>
public sealed record InputBenchmarkResult(
    int EventCount,
    TimeSpan Duration,
    double? AverageIntervalMs,
    double? MinimumIntervalMs,
    double? MaximumIntervalMs,
    double? JitterMs,
    double? EstimatedPollingHz,
    double? StabilityPercent,
    IReadOnlyList<double> IntervalsMs)
{
    public const int AnalyzerVersion = 1;
    public double? MedianIntervalMs { get; init; }
    public double? ObservedEventRateHz { get; init; }
    public double? P95IntervalMs { get; init; }
    public double? P99IntervalMs { get; init; }
    public int OutlierCount { get; init; }
    public int LargeGapCount { get; init; }
    public SampleQuality SampleQuality { get; init; } = SampleQuality.Insufficient;
    public string? DevicePath { get; init; }
    public string DeviceDisplayName { get; init; } = "Generic HID Device";

    public static InputBenchmarkResult Empty(TimeSpan duration)
        => new(0, duration, null, null, null, null, null, null, []);
}

public enum SampleQuality { Insufficient, Low, Fair, Good, High }

public static class InputConsistencyAnalyzer
{
    public const double LargeGapThresholdMs = 20d;

    public static InputBenchmarkResult Analyze(IReadOnlyList<double> intervalsMs, TimeSpan duration, int eventCount, string? devicePath = null)
    {
        var values = intervalsMs.Where(value => double.IsFinite(value) && value >= 0).OrderBy(value => value).ToArray();
        if (values.Length == 0)
            return InputBenchmarkResult.Empty(duration) with { EventCount = eventCount, DevicePath = devicePath, SampleQuality = Quality(duration, eventCount, 0, 0) };

        var mean = values.Average();
        var variance = values.Select(value => Math.Pow(value - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        var median = Percentile(values, .50);
        var largeGaps = values.Count(value => value >= LargeGapThresholdMs);
        var outliers = values.Count(value => value > median + Math.Max(0.25d, 3d * stdDev));
        var observedRate = duration.TotalSeconds > 0 ? eventCount / duration.TotalSeconds : null;
        var estimatedEventFrequency = mean > 0 ? 1000d / mean : null;
        var stability = mean > 0 ? Math.Clamp(100d - (stdDev / mean * 100d) - (largeGaps * 2d), 0d, 100d) : null;

        return new InputBenchmarkResult(eventCount, duration, mean, values[0], values[^1], stdDev, estimatedEventFrequency, stability, values)
        {
            MedianIntervalMs = median,
            ObservedEventRateHz = observedRate,
            P95IntervalMs = Percentile(values, .95),
            P99IntervalMs = Percentile(values, .99),
            OutlierCount = outliers,
            LargeGapCount = largeGaps,
            SampleQuality = Quality(duration, eventCount, largeGaps, stability ?? 0),
            DevicePath = devicePath
        };
    }

    public static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) throw new ArgumentException("A percentile requires at least one value.", nameof(sortedValues));
        var index = (sortedValues.Count - 1) * Math.Clamp(percentile, 0d, 1d);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        return lower == upper ? sortedValues[lower] : sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (index - lower);
    }

    private static SampleQuality Quality(TimeSpan duration, int events, int gaps, double stability)
    {
        if (duration.TotalSeconds < 1 || events < 20) return SampleQuality.Insufficient;
        if (duration.TotalSeconds < 2 || events < 100) return SampleQuality.Low;
        if (duration.TotalSeconds < 4 || events < 500) return SampleQuality.Fair;
        if (gaps > 3 || stability < 70) return SampleQuality.Good;
        return SampleQuality.High;
    }
}

public static class InputBenchmarkCalculator
{
    public static InputBenchmarkResult Calculate(IReadOnlyList<double> intervalsMs, TimeSpan duration, int eventCount)
        => InputConsistencyAnalyzer.Analyze(intervalsMs, duration, eventCount);
}
