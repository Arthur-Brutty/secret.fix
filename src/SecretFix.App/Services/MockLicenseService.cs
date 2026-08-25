using SecretFix.Core;

namespace SecretFix.Services;

public sealed class MockLicenseService : ILicenseService
{
    public const string DevelopmentUsername = "LocalDeveloper";

    private LicenseInfo? _current;

    public Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_current ?? CreateLicense(DevelopmentUsername));

    public Task<LicenseInfo?> SignInAsync(string username, string licenseKey, CancellationToken cancellationToken = default)
    {
        // This is deliberately local-only. It demonstrates the client boundary without
        // pretending that a production licensing backend or credential exists in the repo.
        var normalizedUser = string.IsNullOrWhiteSpace(username) ? DevelopmentUsername : username.Trim();
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
            AppVersion: AppBuildInfo.InformationalVersion,
            Status: "ACTIVE",
            DeviceBindUsed: 1,
            DeviceBindLimit: 1,
            SupportId: "SF-847291");
}
