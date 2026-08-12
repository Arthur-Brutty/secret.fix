using System.IO;
using System.Windows;
using System.Windows.Input;
using SecretFix.Services;

namespace SecretFix;

public partial class LoginWindow : Window
{
    private readonly ILicenseService _licenseService;
    private readonly AppLogService _log = new();

    public LoginWindow()
        : this(new MockLicenseService())
    {
    }

    public LoginWindow(ILicenseService licenseService)
    {
        InitializeComponent();
        _licenseService = licenseService;
        Loaded += (_, _) => StartBackgroundVideo();
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Validando sessao local...";
            var license = await _licenseService.SignInAsync(UsernameBox.Text, LicenseBox.Password);
            if (license is null)
            {
                _log.Info($"Login failed. User={UsernameBox.Text}; KeyLength={LicenseBox.Password.Length}");
                StatusText.Text = "Credencial mock invalida.";
                return;
            }

            _log.Info($"Login success. User={license.Username}; Plan={license.Plan}; KeyMasked={license.MaskedLicense}");
            var main = new MainWindow(license, _licenseService);
            main.Show();
            Close();
        }
        catch (Exception ex)
        {
            _log.Error("Login failed with exception", ex);
            StatusText.Text = "Falha no login. Erro registrado.";
        }
    }

    private void StartBackgroundVideo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Backgrounds", "red-galaxy.mp4");
        if (!File.Exists(path))
        {
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
            _log.Error("Login background failed", ex);
            GalaxyBackground.Visibility = Visibility.Collapsed;
        }
    }

    private void GalaxyBackground_MediaEnded(object sender, RoutedEventArgs e)
    {
        GalaxyBackground.Position = TimeSpan.Zero;
        GalaxyBackground.Play();
    }

    private void GalaxyBackground_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _log.Error("Login background playback failed", e.ErrorException);
        GalaxyBackground.Visibility = Visibility.Collapsed;
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
