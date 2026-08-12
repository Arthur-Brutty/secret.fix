namespace SecretFix.Core;

public sealed record DetectedDevice(
    DeviceKind Kind,
    string Manufacturer,
    string ProductName,
    string FriendlyName,
    string? Vid,
    string? Pid,
    KnownDevice? KnownDevice,
    bool IsExactMatch)
{
    public string DisplayName => KnownDevice is not null
        ? $"{KnownDevice.Manufacturer} {KnownDevice.Model}"
        : string.IsNullOrWhiteSpace(ProductName) ? "Dispositivo HID" : ProductName;
}
