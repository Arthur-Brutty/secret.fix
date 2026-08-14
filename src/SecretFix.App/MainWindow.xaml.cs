using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SecretFix.Controls;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.Views;

namespace SecretFix;

public partial class MainWindow : Window
{
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private readonly SettingsService _settings;
    private readonly ILicenseService _licenseService;
    private readonly LicenseInfo _license;
    private readonly Dictionary<string, UserControl> _views = new(StringComparer.OrdinalIgnoreCase);
    private Button? _activeButton;
    private string _currentPage = "";
    private bool _isFullscreen;
    private WindowState _preFullscreenState;
    private ResizeMode _preFullscreenResizeMode;

    public MainWindow(LicenseInfo license, ILicenseService licenseService)
    {
        _log = new AppLogService();
        _backup = new BackupService(_log);
        _settings = new SettingsService(_log);
        InitializeComponent();
        _license = license;
        _licenseService = licenseService;
        Loaded += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
        KeyDown += MainWindow_KeyDown;
        NotificationService.Requested += ShowToast;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartBackgroundVideo();
        _log.Info($"secret.fix start. Version=v0.4-test; User={_license.Username}; Plan={_license.Plan}; Status={_license.Status}");
        UserText.Text = _license.Username;
        PlanText.Text = _license.Plan.ToString().ToUpperInvariant();
        VersionText.Text = "v0.4 test build";
        ApplyFeatureGates();
        await RunSplashAsync();
        Navigate("Mouse");
    }

    protected override void OnClosed(EventArgs e)
    {
        NotificationService.Requested -= ShowToast;
        _settings.Save();
        CrosshairOverlayService.Close();
        GalaxyBackground.Stop();
        base.OnClosed(e);
    }

    private void StartBackgroundVideo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Backgrounds", "red-galaxy.mp4");
        if (!File.Exists(path))
        {
            _log.Info($"Background video missing. Path={path}");
            GalaxyBackground.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            GalaxyBackground.Source = new Uri(path, UriKind.Absolute);
            GalaxyBackground.Volume = 0;
            GalaxyBackground.IsMuted = true;
            GalaxyBackground.Play();
        }
        catch (Exception ex)
        {
            _log.Error("Background video failed to start", ex);
            GalaxyBackground.Visibility = Visibility.Collapsed;
        }
    }

    private async Task RunSplashAsync()
    {
        SplashLayer.Opacity = 0;
        SplashLayer.Visibility = Visibility.Visible;
        SplashLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)));
        SplashScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(480)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        SplashScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(480)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        await Task.Delay(850);

        AppShell.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)));
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(240));
        fade.Completed += (_, _) => SplashLayer.Visibility = Visibility.Collapsed;
        SplashLayer.BeginAnimation(OpacityProperty, fade);
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            Navigate(element.Tag?.ToString() ?? "Mouse");
    }

    private void Navigate(string page)
    {
        if (page == _currentPage)
            return;

        try
        {
            var feature = FeatureForPage(page);
            var allowed = feature is null || FeatureCatalog.IsAllowed(_license.Plan, feature.Value);
            var minimumPlan = feature is null ? PlanTier.Core : FeatureCatalog.MinimumPlan(feature.Value);

            var view = GetOrCreateView(page, allowed, minimumPlan);

            var fadeOut = new DoubleAnimation(MainContent.Opacity, 0, TimeSpan.FromMilliseconds(80));
            fadeOut.Completed += (_, _) =>
            {
                MainContent.Content = view;
                MainContent.Opacity = 0;
                MainContent.RenderTransform = new TranslateTransform(0, 8);
                MainContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                ((TranslateTransform)MainContent.RenderTransform).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(160)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };

            MainContent.BeginAnimation(OpacityProperty, fadeOut);
            _currentPage = page;
            SetActiveButton(page);
        }
        catch (Exception ex)
        {
            _log.Error($"Navigation failed. Page={page}", ex);
            NotificationService.Show($"Falha ao abrir {page}. Erro registrado.");
        }
    }

    private UserControl GetOrCreateView(string page, bool allowed, PlanTier minimumPlan)
    {
        if (_views.TryGetValue(page, out var cached))
            return cached;

        UserControl view = page switch
        {
            "Mouse" => new MouseFixView(_backup, _log, _settings),
            "Keyboard" => new KeyboardFixView(_backup, _log, _settings, allowed, minimumPlan),
            "FiveM" => new FiveMView(allowed, minimumPlan, _log, _settings),
            "Flick" => new FlickTrainerView(allowed, minimumPlan),
            "Sensi" => new SensiView(_backup, _log, allowed, minimumPlan),
            "Aim" => new AimView(allowed, minimumPlan, _settings, _log),
            "Services" => new ServicesView(allowed, minimumPlan, _settings, _log),
            "Display" => new DisplayView(allowed, minimumPlan, _settings, _log),
            "Account" => CreateAccountView(),
            _ => new PlaceholderView(page, minimumPlan, allowed)
        };

        _views[page] = view;
        return view;
    }

    private AccountView CreateAccountView()
    {
        var view = new AccountView(_license);
        view.SignOutRequested += (_, _) => SignOut();
        view.FullscreenRequested += (_, _) => ToggleFullscreen();
        return view;
    }

    private void ApplyFeatureGates()
    {
        SetGate(MouseBadge, FeatureId.MouseFix);
        SetGate(KeyboardBadge, FeatureId.KeyboardFix);
        SetGate(FiveMBadge, FeatureId.FiveM);
        SetGate(FlickBadge, FeatureId.FlickTrainer);
        SetGate(SensiBadge, FeatureId.Sensitivity);
        SetGate(AimBadge, FeatureId.Aim);
        SetGate(ServicesBadge, FeatureId.Services);
        SetGate(DisplayBadge, FeatureId.DisplayTuning);
    }

    private void SetGate(TextBlock badge, FeatureId feature)
    {
        var allowed = FeatureCatalog.IsAllowed(_license.Plan, feature);
        badge.Text = allowed ? "" : FeatureCatalog.MinimumPlan(feature) switch
        {
            PlanTier.Pulse => "PULSE+",
            PlanTier.Apex => "APEX ONLY",
            _ => "CORE"
        };
    }

    private void SetActiveButton(string page)
    {
        if (_activeButton is not null)
        {
            _activeButton.Background = Brushes.Transparent;
            _activeButton.BorderBrush = Brushes.Transparent;
            _activeButton.Foreground = (Brush)FindResource("MutedBrush");
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
            _activeButton.Background = (Brush)FindResource("PanelHoverBrush");
            _activeButton.BorderBrush = (Brush)FindResource("AccentBrush");
            _activeButton.Foreground = (Brush)FindResource("TextBrush");
        }
    }

    private void ShowToast(string message)
    {
        Dispatcher.Invoke(() =>
        {
            while (ToastHost.Children.Count >= 3)
                ToastHost.Children.RemoveAt(ToastHost.Children.Count - 1);

            var toast = new NotificationToast(message) { Margin = new Thickness(0, 0, 0, 8) };
            toast.Closed += (_, _) => ToastHost.Children.Remove(toast);
            ToastHost.Children.Insert(0, toast);
        });
    }

    private void GalaxyBackground_MediaEnded(object sender, RoutedEventArgs e)
    {
        GalaxyBackground.Position = TimeSpan.Zero;
        GalaxyBackground.Play();
    }

    private void GalaxyBackground_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _log.Error("Background video playback failed", e.ErrorException);
        GalaxyBackground.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            GalaxyBackground.Pause();
        else if (GalaxyBackground.Source is not null)
            GalaxyBackground.Play();

        MaximizeButton.Content = WindowState == WindowState.Maximized ? "[]" : "[]";
    }

    private void DragHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        if (_isFullscreen)
        {
            ToggleFullscreen();
            return;
        }

        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void SignOut()
    {
        _licenseService.SignOut();
        CrosshairOverlayService.Close();
        var login = new LoginWindow();
        login.Show();
        Close();
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _preFullscreenState = WindowState;
            _preFullscreenResizeMode = ResizeMode;
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
            NotificationService.Show("Tela cheia ativada. Pressione Esc para sair.");
        }
        else
        {
            ResizeMode = _preFullscreenResizeMode;
            WindowState = _preFullscreenState;
            _isFullscreen = false;
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isFullscreen)
            ToggleFullscreen();
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
