using SecretFix.Infrastructure.Windows;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Tests;

public sealed class PersistenceAndBackupTests
{
    [Fact]
    public void SettingsService_RoundTripsPersistentState()
    {
        using var scope = new TempScope();
        var path = Path.Combine(scope.Path, "settings.json");
        var first = new SettingsService(new AppLogService(Path.Combine(scope.Path, "logs")), path);
        first.Current.MouseFix.SelectedDeviceId = "Razer|Viper V3 Pro";
        first.Current.KeyboardFix.MinimumDelay = false;
        first.Current.Aim.Preset = AimPreset.High;
        first.Current.Display.Preset = DisplayPreset.Vibrant;
        first.Current.Services.GameBar = true;

        Assert.True(first.Save());
        var second = new SettingsService(new AppLogService(Path.Combine(scope.Path, "logs")), path);

        Assert.Equal("Razer|Viper V3 Pro", second.Current.MouseFix.SelectedDeviceId);
        Assert.False(second.Current.KeyboardFix.MinimumDelay);
        Assert.Equal(AimPreset.High, second.Current.Aim.Preset);
        Assert.Equal(DisplayPreset.Vibrant, second.Current.Display.Preset);
        Assert.True(second.Current.Services.GameBar);
    }

    [Fact]
    public void SettingsService_IgnoresCorruptJson()
    {
        using var scope = new TempScope();
        var path = Path.Combine(scope.Path, "settings.json");
        File.WriteAllText(path, "{ not valid json");

        var service = new SettingsService(new AppLogService(Path.Combine(scope.Path, "logs")), path);

        Assert.NotNull(service.Current);
        Assert.Equal(2, service.Current.SchemaVersion);
    }

    [Fact]
    public void SettingsService_NormalizesPartialAndOutOfRangeValues()
    {
        using var scope = new TempScope();
        var path = Path.Combine(scope.Path, "settings.json");
        File.WriteAllText(path, """
        {
          "SchemaVersion": 1,
          "MouseFix": null,
          "Aim": { "Preset": "Custom", "Size": 999, "Thickness": -2, "Gap": 80, "Opacity": 1, "Color": "Blue" },
          "Display": { "Preset": "Vibrant", "Saturation": 500, "Temperature": -10, "Gamma": 101 }
        }
        """);

        var service = new SettingsService(new AppLogService(Path.Combine(scope.Path, "logs")), path);

        Assert.NotNull(service.Current.MouseFix);
        Assert.Equal(80, service.Current.Aim.Size);
        Assert.Equal(1, service.Current.Aim.Thickness);
        Assert.Equal(30, service.Current.Aim.Gap);
        Assert.Equal(20, service.Current.Aim.Opacity);
        Assert.Equal("Red", service.Current.Aim.Color);
        Assert.Equal(100, service.Current.Display.Saturation);
        Assert.Equal(0, service.Current.Display.Temperature);
        Assert.Equal(100, service.Current.Display.Gamma);
    }

    [Fact]
    public void BackupService_SkipsNewestCorruptBackupAndLoadsValidSnapshot()
    {
        using var scope = new TempScope();
        var service = new BackupService(new AppLogService(Path.Combine(scope.Path, "logs")), scope.Path);
        var expected = new MouseSnapshot(1, 2, 1, 11);
        var validPath = service.SaveMouse(expected);
        File.SetLastWriteTimeUtc(validPath, DateTime.UtcNow.AddMinutes(-1));
        var corruptPath = Path.Combine(scope.Path, $"mouse-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
        File.WriteAllText(corruptPath, "{corrupt");
        File.SetLastWriteTimeUtc(corruptPath, DateTime.UtcNow);

        var loaded = service.LoadLatestMouse();

        Assert.Equal(expected, loaded);
        var log = File.ReadAllText(Path.Combine(scope.Path, "logs", "secretfix.log"));
        Assert.Contains("Backup ignored", log);
    }

    private sealed class TempScope : IDisposable
    {
        public TempScope()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SecretFix.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
