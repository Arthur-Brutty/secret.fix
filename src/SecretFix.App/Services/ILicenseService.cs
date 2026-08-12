using SecretFix.Core;

namespace SecretFix.Services;

public interface ILicenseService
{
    Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<LicenseInfo?> SignInAsync(string username, string licenseKey, CancellationToken cancellationToken = default);
    void SignOut();
}
