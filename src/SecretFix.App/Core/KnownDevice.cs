namespace SecretFix.Core;

public sealed record KnownDevice(
    DeviceKind Kind,
    string Manufacturer,
    string Model,
    string Vid,
    string Pid,
    string AssetPath,
    IReadOnlyList<string>? Aliases = null);
