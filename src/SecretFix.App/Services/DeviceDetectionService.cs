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

        devices = devices
            .GroupBy(device => $"{device.Vid}|{device.Pid}|{device.ProductName}|{device.FriendlyName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(device => device.IsExactMatch)
            .ThenBy(device => device.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var device in devices.Take(12))
            _log.Info($"Detected {kind} VID={device.Vid ?? "unknown"} PID={device.Pid ?? "unknown"} Manufacturer={device.Manufacturer} Product={device.ProductName} Friendly={device.FriendlyName} Exact={device.IsExactMatch}");

        if (devices.Count == 0)
        {
            _log.Info($"No exact {kind} device identity was available. Generic fallback selected.");
            return [CreateGeneric(kind)];
        }

        return devices;
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
                var manufacturer = CleanName(ReadString(instance, "Mfg"));
                if (!LooksLikeKind(kind, className, service, friendly, deviceDesc))
                    continue;

                var hardwareIds = instance.GetValue("HardwareID") as string[];
                var identitySource = hardwareIds?.FirstOrDefault(id => VidPidRegex.IsMatch(id)) ?? hardwareKeyName;
                var (vid, pid) = ParseVidPid(identitySource);
                var known = KnownDevices.Match(kind, vid, pid);
                var productName = !string.IsNullOrWhiteSpace(friendly) ? friendly : CleanName(deviceDesc);
                devices.Add(new DetectedDevice(
                    kind,
                    known?.Manufacturer ?? manufacturer,
                    known?.Model ?? (string.IsNullOrWhiteSpace(productName) ? "Dispositivo HID" : productName),
                    string.IsNullOrWhiteSpace(friendly) ? productName : friendly,
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

    public static (string? Vid, string? Pid) ParseVidPid(string value)
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
        return new DetectedDevice(
            kind,
            source?.Manufacturer ?? "",
            source?.ProductName ?? "Dispositivo HID",
            source?.FriendlyName ?? "Modelo exato não identificado",
            source?.Vid,
            source?.Pid,
            null,
            false);
    }
}
