using SecretFix.Core;

namespace SecretFix.Services;

public sealed class MockLicenseService : ILicenseService
{
    public Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var info = new LicenseInfo(
            Username: "SecretUser_01",
            MaskedLicense: "SF-APX-••••-••••-8K2P",
            Plan: PlanTier.Apex,
            Expiration: null,
            Device: Environment.MachineName,
            AppVersion: "v0.2",
            Status: "ACTIVE",
            DeviceBindUsed: 1,
            DeviceBindLimit: 1,
            SupportId: "SF-847291");

        return Task.FromResult(info);
    }
}
