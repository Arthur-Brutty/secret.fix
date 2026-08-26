using SecretFix.Core;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Tests;

public sealed class PrecisionV06Tests
{
    [Fact]
    public void Analyzer_CalculatesMedianPercentilesOutliersAndGaps()
    {
        var result = InputConsistencyAnalyzer.Analyze([1d, 1d, 1d, 2d, 30d], TimeSpan.FromSeconds(5), 6, "\\??\\HID#VID_1234&PID_5678");
        Assert.Equal(1d, result.MedianIntervalMs);
        Assert.Equal(24.4d, result.P95IntervalMs!.Value, 3);
        Assert.Equal(28.88d, result.P99IntervalMs!.Value, 3);
        Assert.Equal(1, result.LargeGapCount);
        Assert.Equal("\\??\\HID#VID_1234&PID_5678", result.DevicePath);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Analyzer_HandlesSmallSamples(int eventCount)
    {
        var intervals = eventCount < 2 ? Array.Empty<double>() : new[] { 1d };
        var result = InputConsistencyAnalyzer.Analyze(intervals, TimeSpan.FromMilliseconds(500), eventCount);
        Assert.Equal(SampleQuality.Insufficient, result.SampleQuality);
    }

    [Fact]
    public void EvidenceCatalog_OnlyDocumentedSafeEntriesAreAutomatic()
    {
        Assert.NotEmpty(TweakEvidenceCatalog.AutomaticProfileTweaks);
        Assert.All(TweakEvidenceCatalog.AutomaticProfileTweaks, item => Assert.Equal(TweakEvidenceLevel.Documented, item.Evidence));
        Assert.Contains(TweakEvidenceCatalog.All, item => item.Id == "realtime-priority" && item.Evidence == TweakEvidenceLevel.Rejected);
    }

    [Fact]
    public void PrecisionEngine_DetectsPointerConfigurationDrift()
    {
        var engine = new PrecisionEngineService();
        var drift = engine.GetDrift(OptimizationProfile.Competitive, new MouseSnapshot(6, 10, 1, 14));
        Assert.True(drift.Detected);
        Assert.Contains("Pointer speed 10", drift.Expected);
    }

    [Fact]
    public void ProfileState_SerializesPerDeviceSelection()
    {
        var state = new ProfileState();
        state.MouseByDevice["VID_046D&PID_C09B"] = OptimizationProfile.Competitive;
        Assert.Equal(OptimizationProfile.Competitive, state.MouseByDevice["vid_046d&pid_c09b"]);
    }
}
