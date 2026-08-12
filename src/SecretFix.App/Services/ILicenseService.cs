using SecretFix.Core;

namespace SecretFix.Services;

public interface ILicenseService
{
    Task<LicenseInfo> GetCurrentAsync(CancellationToken cancellationToken = default);
}
