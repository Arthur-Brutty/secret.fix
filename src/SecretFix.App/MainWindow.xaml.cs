using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.Views;

namespace SecretFix;

public partial class MainWindow : Window
{
    private readonly BackupService _backup = new();
    private readonly AppLogService _log = new();
    private readonly ILicenseService _licenseService = new MockLicenseService();
    private LicenseInfo? _license;
    private Button? _activeButton;

    public MainWindow()
    {
        InitializeComponent();
        Opacity = 0;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        _license = await _licenseService.GetCurrentAsync();
        UserText.Text = _license.Username;
        PlanText.Text = $"{_license.Plan.ToString().ToUpperInvariant()} - {_license.Status}";
        VersionText.Text = _license.AppVersion;
        ApplyFeatureGates();
        Navigate("Mouse");
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            Navigate(element.Tag?.ToString() ?? "Mouse");
    }

    private void Navigate(string page)
    {
        if (_license is null)
            return;

        var feature = FeatureForPage(page);
        var allowed = feature is null || FeatureCatalog.IsAllowed(_license.Plan, feature.Value);
        var minimumPlan = feature is null ? PlanTier.Core : FeatureCatalog.MinimumPlan(feature.Value);

        MainContent.Content = page switch
        {
            "Mouse" when allowed => new MouseFixView(_backup, _log),
            "Keyboard" when allowed => new KeyboardFixView(_backup, _log),
            "Account" => new AccountView(_license),
            "FiveM" => new PlaceholderView("FiveM", minimumPlan, allowed),
            "Flick" => new PlaceholderView("Flick", minimumPlan, allowed),
            "Sensi" => new PlaceholderView("Sensi", minimumPlan, allowed),
            "Aim" => new PlaceholderView("Mira", minimumPlan, allowed),
            "Services" => new PlaceholderView("Servicos", minimumPlan, allowed),
            "Display" => new PlaceholderView("Display", minimumPlan, allowed),
            _ => new PlaceholderView(page, minimumPlan, allowed)
        };

        SetActiveButton(page);
    }

    private void ApplyFeatureGates()
    {
        if (_license is null)
            return;

        SetGate(MouseButton, FeatureId.MouseFix);
        SetGate(KeyboardButton, FeatureId.KeyboardFix);
        SetGate(FiveMButton, FeatureId.FiveM);
        SetGate(FlickButton, FeatureId.FlickTrainer);
        SetGate(DisplayButton, FeatureId.DisplayTuning);

        SetGate(SensiButton, FeatureId.Sensitivity);
        SetGate(AimButton, FeatureId.Aim);
        SetGate(ServicesButton, FeatureId.Services);
    }

    private void SetGate(Button button, FeatureId feature)
    {
        if (_license is null)
            return;

        var allowed = FeatureCatalog.IsAllowed(_license.Plan, feature);
        var minimum = FeatureCatalog.MinimumPlan(feature).ToString().ToUpperInvariant();
        button.IsEnabled = allowed;
        if (!allowed && button.Content is string content && !content.Contains(minimum, StringComparison.OrdinalIgnoreCase))
            button.Content = $"{content}  {minimum}+";
    }

    private void SetActiveButton(string page)
    {
        if (_activeButton is not null)
        {
            _activeButton.Background = Brushes.Transparent;
            _activeButton.BorderBrush = (Brush)FindResource("BorderBrush");
        }

        _activeButton = page switch
        {
            "Mouse" => MouseButton,
            "Keyboard" => KeyboardButton,
            "FiveM" => FiveMButton,
            "Flick" => FlickButton,
            "Sensi" => SensiButton,
            "Aim" => AimButton,
            "Services" => ServicesButton,
            "Display" => DisplayButton,
            "Account" => AccountButton,
            _ => null
        };

        if (_activeButton is not null)
        {
            _activeButton.Background = (Brush)FindResource("DangerWashBrush");
            _activeButton.BorderBrush = (Brush)FindResource("AccentBrush");
        }
    }

    private static FeatureId? FeatureForPage(string page) => page switch
    {
        "Mouse" => FeatureId.MouseFix,
        "Keyboard" => FeatureId.KeyboardFix,
        "FiveM" => FeatureId.FiveM,
        "Flick" => FeatureId.FlickTrainer,
        "Sensi" => FeatureId.Sensitivity,
        "Aim" => FeatureId.Aim,
        "Services" => FeatureId.Services,
        "Display" => FeatureId.DisplayTuning,
        _ => null
    };
}
