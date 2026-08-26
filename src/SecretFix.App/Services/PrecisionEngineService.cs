using System.IO;
using System.Text.Json;
using SecretFix.Core;
using SecretFix.Infrastructure.Windows;
using SecretFix.State;

namespace SecretFix.Services;

public sealed record ConfigurationDrift(bool Detected, string Expected, string Current, string Message);

/// <summary>Coordinates read-only diagnostics and safe, documented pointer profile expectations.</summary>
public sealed class PrecisionEngineService
{
    private readonly WindowsInputService _pointer = new();

    public MouseSnapshot ReadWindowsPointer() => _pointer.ReadMouse();

    public ConfigurationDrift GetDrift(OptimizationProfile profile, MouseSnapshot current)
    {
        if (profile == OptimizationProfile.Custom)
            return new(false, "Custom profile", ProfileOperationService.Describe(current), "Custom does not declare an automatic pointer target.");
        var expectedSpeed = profile == OptimizationProfile.Competitive ? 10 : current.Speed;
        var expected = $"Pointer speed {expectedSpeed}; acceleration OFF; thresholds 0/0";
        var drift = current.Acceleration != 0 || current.Threshold1 != 0 || current.Threshold2 != 0 || current.Speed != expectedSpeed;
        return new(drift, expected, ProfileOperationService.Describe(current), drift ? "CONFIGURATION DRIFT DETECTED" : "Expected profile matches current system state.");
    }

    public async Task<string> ExportBenchmarkAsync(InputBenchmarkResult result, string destination, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            analyzerVersion = InputBenchmarkResult.AnalyzerVersion,
            device = result.DeviceDisplayName,
            rawInputDevicePath = result.DevicePath,
            durationMs = Math.Round(result.Duration.TotalMilliseconds, 3),
            eventCount = result.EventCount,
            observedEventRateHz = result.ObservedEventRateHz,
            estimatedEventFrequencyHz = result.EstimatedPollingHz,
            meanIntervalMs = result.AverageIntervalMs,
            medianIntervalMs = result.MedianIntervalMs,
            minimumIntervalMs = result.MinimumIntervalMs,
            maximumIntervalMs = result.MaximumIntervalMs,
            standardDeviationMs = result.JitterMs,
            p95IntervalMs = result.P95IntervalMs,
            p99IntervalMs = result.P99IntervalMs,
            outlierCount = result.OutlierCount,
            largeGapCount = result.LargeGapCount,
            stabilityScore = result.StabilityPercent,
            sampleQuality = result.SampleQuality.ToString().ToUpperInvariant(),
            note = "Observed Windows Raw Input event cadence; not a claim about configured USB firmware polling or total latency."
        };
        await using var stream = File.Create(destination);
        await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        return destination;
    }
}
