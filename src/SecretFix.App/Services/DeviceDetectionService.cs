using System.Text.RegularExpressions;
using Microsoft.Win32;
using SecretFix.Core;

namespace SecretFix.Services;

public sealed class DeviceDetectionService
{
    private static readonly Regex VidPidRegex = new(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AppLogService _log;

    public DeviceDetectionService(AppLogService log)
    {
        _log = log;
    }

    public Task<IReadOnlyList<DetectedDevice>> DetectAsync(DeviceKind kind, CancellationToken cancellationToken = default)
        => Task.Run(() => Detect(kind, cancellationToken), cancellationToken);

    public DetectedDevice Simulate(DeviceKind kind, string hardwareId)
    {
        var (vid, pid) = ParseVidPid(hardwareId);
        var known = KnownDevices.Match(kind, vid, pid);
        return new DetectedDevice(kind, known?.Manufacturer ?? "", known?.Model ?? "Dispositivo HID", hardwareId, vid, pid, known, known is not null);
    }

    private IReadOnlyList<DetectedDevice> Detect(DeviceKind kind, CancellationToken cancellationToken)
    {
        var devices = new List<DetectedDevice>();
        try
        {
            using var hid = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\HID");
            if (hid is not null)
                EnumerateRegistry(hid, kind, devices, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error($"Device detection failed. Kind={kind}", ex);
        }

        if (devices.Count == 0)
            devices.Add(CreateGeneric(kind));

        foreach (var device in devices.Take(8))
            _log.Info($"Detected {kind} VID={device.Vid ?? "unknown"} PID={device.Pid ?? "unknown"} Name={device.DisplayName} Exact={device.IsExactMatch}");

        var exact = devices.FirstOrDefault(device => device.IsExactMatch);
        if (exact is not null)
            return [exact];

        return [CreateGeneric(kind, devices.FirstOrDefault())];
    }

    private void EnumerateRegistry(RegistryKey root, DeviceKind kind, List<DetectedDevice> devices, CancellationToken cancellationToken)
    {
        foreach (var hardwareKeyName in root.GetSubKeyNames())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var hardwareKey = root.OpenSubKey(hardwareKeyName);
            if (hardwareKey is null)
                continue;

            foreach (var instanceName in hardwareKey.GetSubKeyNames())
            {
                using var instance = hardwareKey.OpenSubKey(instanceName);
                if (instance is null)
                    continue;

                var className = ReadString(instance, "Class");
                var service = ReadString(instance, "Service");
                var friendly = ReadString(instance, "FriendlyName");
                var deviceDesc = ReadString(instance, "DeviceDesc");
                if (!LooksLikeKind(kind, className, service, friendly, deviceDesc))
                    continue;

                var (vid, pid) = ParseVidPid(hardwareKeyName);
                var known = KnownDevices.Match(kind, vid, pid);
                devices.Add(new DetectedDevice(
                    kind,
                    known?.Manufacturer ?? "",
                    known?.Model ?? CleanName(deviceDesc),
                    friendly,
                    vid,
                    pid,
                    known,
                    known is not null));
            }
        }
    }

    private static bool LooksLikeKind(DeviceKind kind, string className, string service, string friendly, string deviceDesc)
    {
        var text = $"{className} {service} {friendly} {deviceDesc}";
        return kind switch
        {
            DeviceKind.Mouse => text.Contains("mouse", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("mouhid", StringComparison.OrdinalIgnoreCase),
            DeviceKind.Keyboard => text.Contains("keyboard", StringComparison.OrdinalIgnoreCase) ||
                                   text.Contains("kbdhid", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static (string? Vid, string? Pid) ParseVidPid(string value)
    {
        var match = VidPidRegex.Match(value);
        return match.Success
            ? (match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.ToUpperInvariant())
            : (null, null);
    }

    private static string ReadString(RegistryKey key, string name)
        => key.GetValue(name)?.ToString() ?? "";

    private static string CleanName(string value)
    {
        var index = value.LastIndexOf(';');
        return index >= 0 && index < value.Length - 1 ? value[(index + 1)..] : value;
    }

    private static DetectedDevice CreateGeneric(DeviceKind kind, DetectedDevice? source = null)
    {
        var known = kind == DeviceKind.Mouse ? KnownDevices.GenericMouse : KnownDevices.GenericKeyboard;
        return new DetectedDevice(kind, "", source?.ProductName ?? known.Model, source?.FriendlyName ?? "Modelo exato nao identificado", source?.Vid, source?.Pid, known, false);
    }
}
