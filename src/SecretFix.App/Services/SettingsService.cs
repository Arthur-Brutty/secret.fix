using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecretFix.State;

namespace SecretFix.Services;

public sealed class SettingsService
{
    private readonly AppLogService _log;
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsService(AppLogService log, string? path = null)
    {
        _log = log;
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecretFix", "settings.json");
        Current = Load();
    }

    public AppSettings Current { get; private set; }

    public bool Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Current, _jsonOptions));
            File.Move(temporaryPath, _path, true);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error("Settings save failed", ex);
            return false;
        }
    }

    private AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        try
        {
            return Normalize(JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), _jsonOptions));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _log.Error($"Settings load failed. Invalid settings ignored. Path={_path}", ex);
            return new AppSettings();
        }
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= new AppSettings();
        // Migrations are additive: older files deserialize with missing members and
        // are completed below instead of being discarded.
        settings.SchemaVersion = 6;
        settings.MouseFix ??= new MouseFixState();
        settings.KeyboardFix ??= new KeyboardFixState();
        settings.FiveM ??= new FiveMState();
        settings.Aim ??= new AimState();
        settings.Services ??= new ServicesState();
        settings.Display ??= new DisplayState();
        settings.Profiles ??= new ProfileState();
        settings.Profiles.MouseByDevice ??= new(StringComparer.OrdinalIgnoreCase);
        settings.Profiles.KeyboardByDevice ??= new(StringComparer.OrdinalIgnoreCase);
        settings.Benchmark ??= new BenchmarkState();
        settings.Precision ??= new PrecisionState();
        if (settings.Precision.MouseDpi is < 1 or > 100000)
            settings.Precision.MouseDpi = null;

        if (string.IsNullOrWhiteSpace(settings.MouseFix.SelectedDeviceId))
            settings.MouseFix.SelectedDeviceId = "secret.fix|Generic";
        if (string.IsNullOrWhiteSpace(settings.KeyboardFix.SelectedDeviceId))
            settings.KeyboardFix.SelectedDeviceId = "secret.fix|Generic Keyboard";

        if (!Enum.IsDefined(settings.Aim.Preset))
            settings.Aim.Preset = AimPreset.Custom;
        settings.Aim.Size = Math.Clamp(settings.Aim.Size, 8, 80);
        settings.Aim.Thickness = Math.Clamp(settings.Aim.Thickness, 1, 8);
        settings.Aim.Gap = Math.Clamp(settings.Aim.Gap, 0, 30);
        settings.Aim.Opacity = Math.Clamp(settings.Aim.Opacity, 20, 100);
        settings.Aim.Color = string.Equals(settings.Aim.Color, "White", StringComparison.OrdinalIgnoreCase) ? "White" : "Red";

        if (!Enum.IsDefined(settings.Display.Preset))
            settings.Display.Preset = DisplayPreset.Normal;
        settings.Display.Saturation = Math.Clamp(settings.Display.Saturation, 0, 100);
        settings.Display.Temperature = Math.Clamp(settings.Display.Temperature, 0, 100);
        settings.Display.Gamma = Math.Clamp(settings.Display.Gamma, 0, 100);
        return settings;
    }
}
