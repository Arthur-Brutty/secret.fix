using SecretFix.Core;

namespace SecretFix.Services;

public sealed class MockLicenseService : ILicenseService
{
    public const string DevUsername = "SecretUser_01";
    public const string DevLicenseKey = "SF-APX-DEMO-8K2P";

    private LicenseInfo? _current;

    public Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_current ?? CreateLicense(DevUsername));

    public Task<LicenseInfo?> SignInAsync(string username, string licenseKey, CancellationToken cancellationToken = default)
    {
        var normalizedUser = string.IsNullOrWhiteSpace(username) ? DevUsername : username.Trim();
        var normalizedKey = licenseKey.Trim();

        if (!string.Equals(normalizedKey, DevLicenseKey, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedKey, "APEX", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<LicenseInfo?>(null);
        }

        _current = CreateLicense(normalizedUser);
        return Task.FromResult<LicenseInfo?>(_current);
    }

    public void SignOut() => _current = null;

    private static LicenseInfo CreateLicense(string username)
        => new(
            Username: username,
            MaskedLicense: "SF-APX-****-****-8K2P",
            Plan: PlanTier.Apex,
            Expiration: null,
            Device: Environment.MachineName,
            AppVersion: "v0.4",
            Status: "ACTIVE",
            DeviceBindUsed: 1,
            DeviceBindLimit: 1,
            SupportId: "SF-847291");
}
