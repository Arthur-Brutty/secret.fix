namespace SecretFix.Core;

public sealed record LicenseInfo(
    string Username,
    string MaskedLicense,
    PlanTier Plan,
    DateTimeOffset? Expiration,
    string Device,
    string AppVersion,
    string Status,
    int DeviceBindUsed,
    int DeviceBindLimit,
    string SupportId);
