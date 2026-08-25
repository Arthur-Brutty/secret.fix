namespace SecretFix.Core;

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
    public static InputBenchmarkResult Empty(TimeSpan duration)
        => new(0, duration, null, null, null, null, null, null, []);
}

public static class InputBenchmarkCalculator
{
    public static InputBenchmarkResult Calculate(IReadOnlyList<double> intervalsMs, TimeSpan duration, int eventCount)
    {
        if (intervalsMs.Count == 0)
            return InputBenchmarkResult.Empty(duration) with { EventCount = eventCount };

        var average = intervalsMs.Average();
        var variance = intervalsMs.Select(value => Math.Pow(value - average, 2)).Average();
        var jitter = Math.Sqrt(variance);
        double? polling = average > 0 ? 1000d / average : null;
        double? stability = average > 0 ? Math.Clamp(100d - (jitter / average * 100d), 0d, 100d) : null;
        return new InputBenchmarkResult(eventCount, duration, average, intervalsMs.Min(), intervalsMs.Max(), jitter, polling, stability, intervalsMs.ToArray());
    }
}
