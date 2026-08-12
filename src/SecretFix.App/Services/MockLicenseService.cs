using SecretFix.Core;

namespace SecretFix.Services;

public sealed class MockLicenseService : ILicenseService
{
    public Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var info = new LicenseInfo(
            Username: "SecretUser",
            MaskedLicense: "SF-APX-****-****-DEMO",
            Plan: PlanTier.Apex,
            Expiration: null,
            Device: Environment.MachineName,
            AppVersion: "0.5.0-dev",
            Status: "APEX MAX (DEV)",
            DeviceBindUsed: 1,
            DeviceBindLimit: 1,
            SupportId: "SF-DEMO");

        return Task.FromResult(info);
    }
}
