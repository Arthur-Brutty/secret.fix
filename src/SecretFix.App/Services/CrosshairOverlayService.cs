using System.Windows.Media;

namespace SecretFix.Services;

public sealed record CrosshairSettings(double Size, double Thickness, double Gap, double Opacity, Color Color);

public static class CrosshairOverlayService
{
    private static CrosshairOverlayWindow? _window;
    private static CrosshairSettings _settings = new(34, 2, 8, 0.9, Color.FromRgb(217, 31, 47));

    public static bool IsActive => _window is not null;

    public static void Show()
    {
        _window ??= new CrosshairOverlayWindow();
        _window.ApplySettings(_settings);
        _window.Show();
    }

    public static void Update(CrosshairSettings settings)
    {
        _settings = settings;
        _window?.ApplySettings(settings);
    }

    public static void Close()
    {
        var window = _window;
        _window = null;
        window?.Close();
    }
}
