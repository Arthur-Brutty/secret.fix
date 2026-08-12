using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SecretFix.Core;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class KeyboardFixView : UserControl
{
    private readonly WindowsKeyboardService _keyboard = new();
    private readonly DeviceDetectionService _deviceDetection;
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private KeyboardSnapshot? _sessionSnapshot;
    private Button? _selectedDevice;
    private bool _suppressSelectionToast;

    public KeyboardFixView(BackupService backup, AppLogService log)
    {
        InitializeComponent();
        _backup = backup;
        _log = log;
        _deviceDetection = new DeviceDetectionService(log);
        BuildDeviceCards();
        SelectDevice(KeyboardWooting);
        Loaded += async (_, _) => await DetectAsync();
    }

    private void BuildDeviceCards()
    {
        foreach (var button in FindVisualChildren<Button>(KeyboardScroll))
        {
            if (button.Tag is not string tag)
                continue;

            var parts = tag.Split('|');
            if (parts.Length != 3)
                continue;

            button.Content = new StackPanel
            {
                Children =
                {
                    new Image
                    {
                        Source = new BitmapImage(new Uri(parts[0], UriKind.Relative)),
                        Height = 68,
                        MaxWidth = 150,
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(0, 2, 0, 12)
                    },
                    new TextBlock
                    {
                        Text = parts[1],
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 13,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = parts[2],
                        Foreground = (Brush)FindResource("MutedBrush"),
                        FontSize = 11,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 150,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            };
            button.MouseEnter += Device_MouseEnter;
            button.MouseLeave += Device_MouseLeave;
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var before = _keyboard.ReadKeyboard();
            _sessionSnapshot ??= before;
            var backupPath = _backup.SaveKeyboard(before);

            _keyboard.ApplyGamingProfile(
                disableFilterKeys: FilterKeysOff.IsChecked == true,
                disableStickyKeys: StickyKeysOff.IsChecked == true,
                disableToggleKeys: ToggleKeysOff.IsChecked == true);

            var after = _keyboard.ReadKeyboard();
            _log.Info($"KeyboardFix applied. Before={before}; After={after}; Backup={backupPath}");
            NotificationService.Show($"TecladoFix aplicado. Speed {after.Speed}, delay {after.Delay}.");
        }
        catch (Exception ex)
        {
            _log.Info($"KeyboardFix apply failed. Error={ex.Message}");
            NotificationService.Show($"Falha ao aplicar TecladoFix: {ex.Message}");
        }
    }

    private void RestoreLatest_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backup.LoadLatestKeyboard();
        if (latest is null)
        {
            NotificationService.Show("Nenhum backup de teclado encontrado.");
            return;
        }

        RestoreSnapshot(latest, "último backup");
    }

    private void Device_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            SelectDevice(button);
    }

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box && box.Content is string label)
            NotificationService.Show($"{ToTitle(label)} selecionado");
    }

    private void SelectDevice(Button button)
    {
        if (button.Tag is not string tag)
            return;

        if (_selectedDevice is not null)
        {
            _selectedDevice.BorderBrush = (Brush)FindResource("BorderBrush");
            _selectedDevice.Background = (Brush)FindResource("PanelBrush");
        }

        _selectedDevice = button;
        button.BorderBrush = (Brush)FindResource("AccentBrush");
        button.Background = (Brush)FindResource("DangerWashBrush");

        var parts = tag.Split('|');
        if (parts.Length == 3)
        {
            HeroKeyboardImage.Source = new BitmapImage(new Uri(parts[0], UriKind.Relative));
            SelectedKeyboardText.Text = $"{parts[1]} {parts[2]}";
            if (!_suppressSelectionToast)
                DetectedKeyboardText.Text = $"Selecionado manualmente: {parts[1]} {parts[2]}";
        }
    }

    private async void DetectAgain_Click(object sender, RoutedEventArgs e) => await DetectAsync();

    private async Task DetectAsync()
    {
        try
        {
            DetectedKeyboardText.Text = "Detectando teclado...";
            var devices = await _deviceDetection.DetectAsync(DeviceKind.Keyboard);
            var detected = devices.FirstOrDefault();
            if (detected is null)
            {
                DetectedKeyboardText.Text = "Teclado detectado: Generic Keyboard";
                return;
            }

            var known = detected.KnownDevice ?? KnownDevices.GenericKeyboard;
            SelectMatchingDevice(known);
            DetectedKeyboardText.Text = detected.IsExactMatch
                ? $"Detectado automaticamente: {known.Manufacturer} {known.Model} (VID={detected.Vid} PID={detected.Pid})"
                : "Teclado detectado: Dispositivo HID - modelo exato nao identificado";
        }
        catch (Exception ex)
        {
            _log.Error("Keyboard detection failed", ex);
            DetectedKeyboardText.Text = "Deteccao falhou. Perfil Generic mantido.";
        }
    }

    private void SelectMatchingDevice(KnownDevice known)
    {
        foreach (var button in FindVisualChildren<Button>(KeyboardScroll))
        {
            if (button.Tag is not string tag)
                continue;

            var parts = tag.Split('|');
            if (parts.Length == 3 &&
                string.Equals(parts[1], known.Manufacturer, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[2], known.Model, StringComparison.OrdinalIgnoreCase))
            {
                _suppressSelectionToast = true;
                SelectDevice(button);
                _suppressSelectionToast = false;
                return;
            }
        }

        _suppressSelectionToast = true;
        SelectDevice(KeyboardGeneric);
        _suppressSelectionToast = false;
    }

    private void RestoreSnapshot(KeyboardSnapshot snapshot, string source)
    {
        try
        {
            _keyboard.Restore(snapshot);
            var after = _keyboard.ReadKeyboard();
            _log.Info($"KeyboardFix restored from {source}. Target={snapshot}; After={after}");
            NotificationService.Show($"Teclado restaurado: speed {after.Speed}, delay {after.Delay}.");
        }
        catch (Exception ex)
        {
            _log.Info($"KeyboardFix restore failed. Source={source}; Error={ex.Message}");
            NotificationService.Show($"Falha ao restaurar teclado: {ex.Message}");
        }
    }

    private void KeyboardPrev_Click(object sender, RoutedEventArgs e) => KeyboardScroll.ScrollToHorizontalOffset(Math.Max(0, KeyboardScroll.HorizontalOffset - 380));

    private void KeyboardNext_Click(object sender, RoutedEventArgs e) => KeyboardScroll.ScrollToHorizontalOffset(KeyboardScroll.HorizontalOffset + 380);

    private static void Device_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.RenderTransform is not TransformGroup group)
            return;

        if (group.Children[0] is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 1.02, TimeSpan.FromMilliseconds(160)));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 1.02, TimeSpan.FromMilliseconds(160)));
        }

        if (group.Children[1] is TranslateTransform translate)
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -3, TimeSpan.FromMilliseconds(160)));
    }

    private static void Device_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.RenderTransform is not TransformGroup group)
            return;

        if (group.Children[0] is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale.ScaleX, 1, TimeSpan.FromMilliseconds(170)));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale.ScaleY, 1, TimeSpan.FromMilliseconds(170)));
        }

        if (group.Children[1] is TranslateTransform translate)
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(translate.Y, 0, TimeSpan.FromMilliseconds(170)));
    }

    private static string ToTitle(string value) => value.Length == 0 ? value : value[0] + value[1..].ToLowerInvariant();

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                yield return typed;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
