using SecretFix.Core;
using SecretFix.Infrastructure.Windows;

namespace SecretFix.Services;

public sealed record MouseDiagnostics(DetectedDevice Device, MouseSnapshot? Settings, string SelectedDevice, string Profile, DateTimeOffset? LastChange);
public sealed record KeyboardDiagnostics(DetectedDevice Device, KeyboardSnapshot? Settings, string SelectedDevice, string Profile, DateTimeOffset? LastChange);

public sealed class DiagnosticsService
{
    private readonly DeviceDetectionService _devices;
    private readonly WindowsInputService _mouse = new();
    private readonly WindowsKeyboardService _keyboard = new();
    private readonly OperationService _operations;
    private readonly AppLogService _log;

    public DiagnosticsService(AppLogService log, OperationService operations)
    {
        _log = log;
        _operations = operations;
        _devices = new DeviceDetectionService(log);
    }

    public async Task<MouseDiagnostics> ReadMouseAsync(string selected, string profile)
    {
        var device = (await _devices.DetectAsync(DeviceKind.Mouse)).FirstOrDefault() ?? _devices.Simulate(DeviceKind.Mouse, "");
        MouseSnapshot? settings = null;
        try { settings = await Task.Run(_mouse.ReadMouse); } catch (Exception ex) { _log.Error("Mouse diagnostics read failed", ex); }
        return new MouseDiagnostics(device, settings, selected, profile, LastChange("Mouse"));
    }

    public async Task<KeyboardDiagnostics> ReadKeyboardAsync(string selected, string profile)
    {
        var device = (await _devices.DetectAsync(DeviceKind.Keyboard)).FirstOrDefault() ?? _devices.Simulate(DeviceKind.Keyboard, "");
        KeyboardSnapshot? settings = null;
        try { settings = await Task.Run(_keyboard.ReadKeyboard); } catch (Exception ex) { _log.Error("Keyboard diagnostics read failed", ex); }
        return new KeyboardDiagnostics(device, settings, selected, profile, LastChange("Keyboard"));
    }

    private DateTimeOffset? LastChange(string module) => _operations.LoadHistory().FirstOrDefault(entry => entry.Module.Equals(module, StringComparison.OrdinalIgnoreCase))?.Timestamp;
}
