using System.Windows.Controls;
using SecretFix.Core;

namespace SecretFix.Views;

public partial class AccountView : UserControl
{
    public event EventHandler? SignOutRequested;
    public event EventHandler? FullscreenRequested;

    public AccountView(LicenseInfo license)
    {
        InitializeComponent();
        UsernameText.Text = license.Username;
        LicenseText.Text = license.MaskedLicense;
        PlanText.Text = license.Plan.ToString().ToUpperInvariant();
        ExpirationText.Text = license.Expiration?.ToString("yyyy-MM-dd") ?? "Lifetime";
        DeviceText.Text = license.Device;
        VersionText.Text = license.AppVersion;
        StatusText.Text = license.Status;
        DeviceBindText.Text = $"{license.DeviceBindUsed} / {license.DeviceBindLimit}";
        SupportText.Text = license.SupportId;
    }

    private void SignOut_Click(object sender, System.Windows.RoutedEventArgs e) => SignOutRequested?.Invoke(this, EventArgs.Empty);

    private void Fullscreen_Click(object sender, System.Windows.RoutedEventArgs e) => FullscreenRequested?.Invoke(this, EventArgs.Empty);

    private void ViewPlans_Click(object sender, System.Windows.RoutedEventArgs e)
        => PlansPanel.Visibility = System.Windows.Visibility.Visible;

    private void ClosePlans_Click(object sender, System.Windows.RoutedEventArgs e)
        => PlansPanel.Visibility = System.Windows.Visibility.Collapsed;
}
