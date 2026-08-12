using System.Windows.Controls;
using SecretFix.Core;

namespace SecretFix.Views;

public partial class AccountView : UserControl
{
    public AccountView(LicenseInfo license)
    {
        InitializeComponent();
        UsernameText.Text = license.Username;
        LicenseText.Text = license.MaskedLicense;
        PlanText.Text = license.Plan.ToString().ToUpperInvariant();
        ExpirationText.Text = license.Expiration?.ToString("yyyy-MM-dd") ?? "Lifetime / dev";
        DeviceText.Text = license.Device;
        VersionText.Text = license.AppVersion;
        StatusText.Text = license.Status;
        DeviceBindText.Text = $"{license.DeviceBindUsed}/{license.DeviceBindLimit}";
        SupportText.Text = license.SupportId;
    }
}
