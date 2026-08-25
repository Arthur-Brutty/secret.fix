using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Tests;

public sealed class V05ReliabilityTests
{
    [Fact]
    public void ProfileCatalog_ListsOnlyTransparentPlannedChanges()
    {
        var balanced = ProfileCatalog.Get(OptimizationProfile.Balanced);
        var competitive = ProfileCatalog.Get(OptimizationProfile.Competitive);
        var custom = ProfileCatalog.Get(OptimizationProfile.Custom);

        Assert.Equal(3, balanced.MouseChanges.Count);
        Assert.Equal(2, balanced.KeyboardChanges.Count);
        Assert.Equal(3, competitive.MouseChanges.Count);
        Assert.Equal(3, competitive.KeyboardChanges.Count);
        Assert.All(competitive.MouseChanges.Concat(competitive.KeyboardChanges), change => Assert.True(change.Supported));
        Assert.Contains("Nenhuma alteração oculta", custom.MouseChanges.Single().Description);
    }

    [Fact]
    public void InputBenchmarkCalculator_CalculatesIntervalsPollingJitterAndStability()
    {
        var result = InputBenchmarkCalculator.Calculate([1d, 2d, 3d], TimeSpan.FromSeconds(2), 4);

        Assert.Equal(4, result.EventCount);
        Assert.Equal(2d, result.AverageIntervalMs);
        Assert.Equal(1d, result.MinimumIntervalMs);
        Assert.Equal(3d, result.MaximumIntervalMs);
        Assert.Equal(500d, result.EstimatedPollingHz);
        Assert.Equal(0.816d, result.JitterMs!.Value, 3);
        Assert.Equal(59.175d, result.StabilityPercent!.Value, 3);
    }

    [Fact]
    public void InputBenchmarkCalculator_HandlesEmptyCapture()
    {
        var result = InputBenchmarkCalculator.Calculate([], TimeSpan.FromSeconds(1), 0);

        Assert.Equal(0, result.EventCount);
        Assert.Null(result.AverageIntervalMs);
        Assert.Null(result.EstimatedPollingHz);
        Assert.Empty(result.IntervalsMs);
    }

    [Fact]
    public void KnownDevices_ProvidesAliasesWithoutRelaxingVidPidMatching()
    {
        var device = KnownDevices.Match(DeviceKind.Mouse, "046D", "C09B");

        Assert.NotNull(device);
        Assert.Contains("G PRO X SUPERLIGHT 2", device.Aliases!);
        Assert.Null(KnownDevices.Match(DeviceKind.Mouse, "046D", "FFFF"));
    }

    [Fact]
    public void OperationService_PersistsPendingThenCompletesIntoHistory()
    {
        using var scope = new TemporaryFolder();
        var log = new AppLogService(Path.Combine(scope.Path, "logs"));
        var service = new OperationService(log, scope.Path);

        var pending = service.Begin("Mouse", "Competitive", "backup.json");
        Assert.Equal(pending, service.GetPending());

        service.Complete(pending, ChangeStatus.Verified, 3, "State was reread.");

        Assert.Null(service.GetPending());
        var entry = Assert.Single(service.LoadHistory());
        Assert.Equal("Mouse", entry.Module);
        Assert.Equal(ChangeStatus.Verified, entry.Status);
        Assert.Equal("backup.json", entry.BackupPath);
    }

    [Fact]
    public void OperationService_IgnoresCorruptHistoryAndPendingFiles()
    {
        using var scope = new TemporaryFolder();
        File.WriteAllText(Path.Combine(scope.Path, "history.json"), "{bad");
        File.WriteAllText(Path.Combine(scope.Path, "pending-operation.json"), "{bad");
        var service = new OperationService(new AppLogService(Path.Combine(scope.Path, "logs")), scope.Path);

        Assert.Empty(service.LoadHistory());
        Assert.Null(service.GetPending());
    }

    [Fact]
    public void SettingsService_MigratesV04FileToV05WithNewState()
    {
        using var scope = new TemporaryFolder();
        var path = Path.Combine(scope.Path, "settings.json");
        File.WriteAllText(path, "{ \"SchemaVersion\": 4, \"MouseFix\": { \"SelectedDeviceId\": \"secret.fix|Generic\" } }");

        var settings = new SettingsService(new AppLogService(Path.Combine(scope.Path, "logs")), path);

        Assert.Equal(5, settings.Current.SchemaVersion);
        Assert.NotNull(settings.Current.Profiles);
        Assert.NotNull(settings.Current.Benchmark);
        Assert.Empty(settings.Current.Profiles.MouseByDevice);
    }

    [Fact]
    public void AppLogService_RedactsCredentialLikeValues()
    {
        const string sensitive = "value-that-must-not-appear";
        var result = AppLogService.Redact("Authorization=Bearer " + sensitive + "; password=" + sensitive + "; LicenseKey=" + sensitive);

        Assert.DoesNotContain(sensitive, result);
        Assert.Contains("[REDACTED]", result);
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SecretFix.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
