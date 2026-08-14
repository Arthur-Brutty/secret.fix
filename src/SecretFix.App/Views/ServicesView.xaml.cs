using System.Windows;
using System.Windows.Controls;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class ServicesView : UserControl
{
    private readonly SettingsService _settings;
    private readonly AppLogService _log;
    private bool _isReady;

    public ServicesView(bool allowed, PlanTier minimumPlan, SettingsService settings, AppLogService log)
    {
        _settings = settings;
        _log = log;
        InitializeComponent();
        var state = _settings.Current.Services;
        BackgroundApps.IsChecked = state.BackgroundApps;
        GameBar.IsChecked = state.GameBar;
        PowerPlan.IsChecked = state.PowerPlan;
        OptionalServices.IsChecked = state.OptionalServices;
        BackgroundApps.IsEnabled = allowed;
        GameBar.IsEnabled = allowed;
        PowerPlan.IsEnabled = allowed;
        OptionalServices.IsEnabled = allowed;
        LockText.Text = allowed ? "EXPERIMENTAL" : $"{minimumPlan.ToString().ToUpperInvariant()}+ ONLY";
        _isReady = true;
    }

    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (!_isReady)
            return;

        var state = _settings.Current.Services;
        state.BackgroundApps = BackgroundApps.IsChecked == true;
        state.GameBar = GameBar.IsChecked == true;
        state.PowerPlan = PowerPlan.IsChecked == true;
        state.OptionalServices = OptionalServices.IsChecked == true;
        _settings.Save();
        _log.Info($"Services preferences updated. BackgroundApps={state.BackgroundApps}; GameBar={state.GameBar}; PowerPlan={state.PowerPlan}; OptionalServices={state.OptionalServices}; AppliedToWindows=false");
    }
}
