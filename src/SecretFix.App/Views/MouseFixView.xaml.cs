using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Views;

public partial class MouseFixView : UserControl
{
    private readonly DeviceDetectionService _deviceDetection;
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private readonly SettingsService _settings;
    private readonly ProfileOperationService _profiles;
    private Button? _selectedDevice;
    private bool _isReady;

    public MouseFixView(BackupService backup, AppLogService log, SettingsService settings, OperationService operations)
    {
        _backup = backup;
        _log = log;
        _settings = settings;
        _profiles = new ProfileOperationService(backup, operations, log);
        _deviceDetection = new DeviceDetectionService(log);
        InitializeComponent();
        BuildDeviceCards();
        LoadState();
        _isReady = true;
    }

    private void BuildDeviceCards()
    {
        foreach (var button in FindVisualChildren<Button>(MouseScroll))
        {
            if (button.Tag is not string tag)
                continue;

            var parts = tag.Split('|');
            if (parts.Length != 3)
                continue;

            button.Content = new Grid
            {
                Children =
                {
                    new StackPanel
                    {
                        Children =
                        {
                            new Image
                            {
                                Source = new BitmapImage(new Uri(parts[0], UriKind.Relative)),
                                Height = 78,
                                MaxWidth = 126,
                                Stretch = Stretch.Uniform,
                                Margin = new Thickness(0, 0, 0, 8)
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
                                MaxWidth = 126,
                                HorizontalAlignment = HorizontalAlignment.Center
                            }
                        }
                    }
                }
            };
            button.MouseEnter += Device_MouseEnter;
            button.MouseLeave += Device_MouseLeave;
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        ApplyMouseButton.IsEnabled = false;
        RestoreMouseButton.IsEnabled = false;
        OperationText.Text = "APLICANDO\nSnapshot, backup, alteração e releitura em andamento.";
        var result = await Task.Run(() => _profiles.ApplyMouse(CurrentProfile, _settings.Current.MouseFix));
        OperationText.Text = $"{result.Status.ToDisplay()}\nANTES: {result.Before}\nDEPOIS: {result.After}\n{result.Message}";
        ApplyMouseButton.IsEnabled = true;
        RestoreMouseButton.IsEnabled = true;
    }

    private async void RestoreLatest_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backup.LoadLatestMouse();
        if (latest is null)
        {
            NotificationService.Show("Nenhum backup de mouse encontrado.");
            return;
        }

        ApplyMouseButton.IsEnabled = false;
        RestoreMouseButton.IsEnabled = false;
        OperationText.Text = "APLICANDO\nRestaurando backup e relendo o estado.";
        var result = await Task.Run(() => _profiles.RestoreMouse(latest, "último backup"));
        OperationText.Text = $"{result.Status.ToDisplay()}\nDEPOIS: {result.After}\n{result.Message}";
        ApplyMouseButton.IsEnabled = true;
        RestoreMouseButton.IsEnabled = true;
    }

    private void Device_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            SelectDevice(button);
    }

    private void Option_Click(object sender, RoutedEventArgs e) => SaveState();

    private void SelectDevice(Button button, bool automatic = false, bool persist = true)
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
            HeroMouseImage.Source = new BitmapImage(new Uri(parts[0], UriKind.Relative));
            SelectedMouseText.Text = $"{parts[1]} {parts[2]}";
            DetectedMouseText.Text = automatic
                ? $"Detectado automaticamente: {parts[1]} {parts[2]}"
                : $"Selecionado manualmente: {parts[1]} {parts[2]}";
            if (_isReady && persist)
            {
                _settings.Current.MouseFix.SelectedDeviceId = $"{parts[1]}|{parts[2]}";
                _settings.Save();
            }
        }
    }

    private async void DetectAgain_Click(object sender, RoutedEventArgs e) => await DetectAsync();

    private async Task DetectAsync()
    {
        try
        {
            DetectedMouseText.Text = "Detectando mouse...";
            var devices = await _deviceDetection.DetectAsync(DeviceKind.Mouse);
            var detected = devices.FirstOrDefault();
            if (detected is null)
            {
                DetectedMouseText.Text = "Mouse detectado: Generic";
                return;
            }

            var known = detected.KnownDevice ?? KnownDevices.GenericMouse;
            SelectMatchingDevice(known, automatic: true);
            DetectedMouseText.Text = detected.IsExactMatch
                ? $"Detectado automaticamente: {known.Manufacturer} {known.Model} (VID={detected.Vid} PID={detected.Pid})"
                : "Mouse detectado: Dispositivo HID - modelo exato nao identificado";
        }
        catch (Exception ex)
        {
            _log.Error("Mouse detection failed", ex);
            DetectedMouseText.Text = "Deteccao falhou. Perfil Generic mantido.";
        }
    }

    private void SelectMatchingDevice(KnownDevice known, bool automatic, bool persist = true)
    {
        foreach (var button in FindVisualChildren<Button>(MouseScroll))
        {
            if (button.Tag is not string tag)
                continue;

            var parts = tag.Split('|');
            if (parts.Length == 3 &&
                string.Equals(parts[1], known.Manufacturer, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[2], known.Model, StringComparison.OrdinalIgnoreCase))
            {
                SelectDevice(button, automatic, persist);
                return;
            }
        }

        SelectDevice(MouseGeneric, automatic, persist);
    }

    private void LoadState()
    {
        var state = _settings.Current.MouseFix;
        MousePrecision.IsChecked = state.MousePrecision;
        PerformanceBoost.IsChecked = state.PerformanceBoost;
        Tracking.IsChecked = state.Tracking;
        Sensi.IsChecked = state.Sensitivity;
        Flick.IsChecked = state.Flick;
        HalfMs.IsChecked = state.HalfMillisecondExperimental;
        RegistryVisual.IsChecked = state.RegistryVisual;
        IslcVisual.IsChecked = state.IslcVisual;
        SensitivityXY.IsChecked = state.SensitivityXY;
        FlagsVisual.IsChecked = state.FlagsVisual;
        AccessibilityVisual.IsChecked = state.AccessibilityVisual;
        FiveMBoostVisual.IsChecked = state.FiveMBoostVisual;

        var parts = state.SelectedDeviceId.Split('|');
        var known = parts.Length == 2
            ? new KnownDevice(DeviceKind.Mouse, parts[0], parts[1], "", "", "")
            : KnownDevices.GenericMouse;
        SelectMatchingDevice(known, automatic: false, persist: false);
        SelectProfile(_settings.Current.Profiles.MouseByDevice.TryGetValue(state.SelectedDeviceId, out var profile) ? profile : _settings.Current.Profiles.MouseProfile);
        DetectedMouseText.Text = $"Seleção salva: {SelectedMouseText.Text}";
    }

    private void SaveState()
    {
        if (!_isReady)
            return;

        var state = _settings.Current.MouseFix;
        state.MousePrecision = MousePrecision.IsChecked == true;
        state.PerformanceBoost = PerformanceBoost.IsChecked == true;
        state.Tracking = Tracking.IsChecked == true;
        state.Sensitivity = Sensi.IsChecked == true;
        state.Flick = Flick.IsChecked == true;
        state.HalfMillisecondExperimental = HalfMs.IsChecked == true;
        state.RegistryVisual = RegistryVisual.IsChecked == true;
        state.IslcVisual = IslcVisual.IsChecked == true;
        state.SensitivityXY = SensitivityXY.IsChecked == true;
        state.FlagsVisual = FlagsVisual.IsChecked == true;
        state.AccessibilityVisual = AccessibilityVisual.IsChecked == true;
        state.FiveMBoostVisual = FiveMBoostVisual.IsChecked == true;
        _settings.Save();
    }

    private OptimizationProfile CurrentProfile => Enum.TryParse<OptimizationProfile>((ProfilePicker.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var profile) ? profile : OptimizationProfile.Balanced;

    private void SelectProfile(OptimizationProfile profile)
    {
        ProfilePicker.SelectedIndex = profile switch { OptimizationProfile.Competitive => 1, OptimizationProfile.Custom => 2, _ => 0 };
    }

    private void ProfilePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        var profile = CurrentProfile;
        _settings.Current.Profiles.MouseProfile = profile;
        _settings.Current.Profiles.MouseByDevice[_settings.Current.MouseFix.SelectedDeviceId] = profile;
        _settings.Save();
        var plan = ProfileCatalog.Get(profile);
        OperationText.Text = $"NÃO APLICADO\n{string.Join(" · ", plan.MouseChanges.Select(change => change.Title))}\n{plan.MouseChanges.Count} alterações planejadas; nenhuma ação oculta.";
    }

    private void MousePrev_Click(object sender, RoutedEventArgs e) => MouseScroll.ScrollToHorizontalOffset(Math.Max(0, MouseScroll.HorizontalOffset - 330));

    private void MouseNext_Click(object sender, RoutedEventArgs e) => MouseScroll.ScrollToHorizontalOffset(MouseScroll.HorizontalOffset + 330);

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
