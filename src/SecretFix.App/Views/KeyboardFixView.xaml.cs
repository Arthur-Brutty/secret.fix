using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Views;

public partial class KeyboardFixView : UserControl
{
    private readonly DeviceDetectionService _deviceDetection;
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private readonly SettingsService _settings;
    private readonly ProfileOperationService _profiles;
    private readonly bool _allowed;
    private readonly PlanTier _minimumPlan;
    private Button? _selectedDevice;
    private bool _isReady;

    public KeyboardFixView(BackupService backup, AppLogService log, SettingsService settings, OperationService operations, bool allowed, PlanTier minimumPlan)
    {
        _backup = backup;
        _log = log;
        _settings = settings;
        _profiles = new ProfileOperationService(backup, operations, log);
        _allowed = allowed;
        _minimumPlan = minimumPlan;
        _deviceDetection = new DeviceDetectionService(log);
        InitializeComponent();
        BuildDeviceCards();
        LoadState();
        _isReady = true;
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

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show($"TecladoFix requer {_minimumPlan.ToString().ToUpperInvariant()}+.");
            return;
        }

        ApplyKeyboardButton.IsEnabled = false;
        RestoreKeyboardButton.IsEnabled = false;
        OperationText.Text = "APLICANDO\nSnapshot, backup, alteração e releitura em andamento.";
        var result = await Task.Run(() => _profiles.ApplyKeyboard(CurrentProfile, _settings.Current.KeyboardFix));
        OperationText.Text = $"{result.Status.ToDisplay()}\nANTES: {result.Before}\nDEPOIS: {result.After}\n{result.Message}";
        ApplyKeyboardButton.IsEnabled = true;
        RestoreKeyboardButton.IsEnabled = true;
    }

    private async void RestoreLatest_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backup.LoadLatestKeyboard();
        if (latest is null)
        {
            NotificationService.Show("Nenhum backup de teclado encontrado.");
            return;
        }

        ApplyKeyboardButton.IsEnabled = false;
        RestoreKeyboardButton.IsEnabled = false;
        OperationText.Text = "APLICANDO\nRestaurando backup e relendo o estado.";
        var result = await Task.Run(() => _profiles.RestoreKeyboard(latest, "último backup"));
        OperationText.Text = $"{result.Status.ToDisplay()}\nDEPOIS: {result.After}\n{result.Message}";
        ApplyKeyboardButton.IsEnabled = true;
        RestoreKeyboardButton.IsEnabled = true;
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
            HeroKeyboardImage.Source = new BitmapImage(new Uri(parts[0], UriKind.Relative));
            SelectedKeyboardText.Text = $"{parts[1]} {parts[2]}";
            DetectedKeyboardText.Text = automatic
                ? $"Detectado automaticamente: {parts[1]} {parts[2]}"
                : $"Selecionado manualmente: {parts[1]} {parts[2]}";
            if (_isReady && persist)
            {
                _settings.Current.KeyboardFix.SelectedDeviceId = $"{parts[1]}|{parts[2]}";
                _settings.Save();
            }
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
            SelectMatchingDevice(known, automatic: true);
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

    private void SelectMatchingDevice(KnownDevice known, bool automatic, bool persist = true)
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
                SelectDevice(button, automatic, persist);
                return;
            }
        }

        SelectDevice(KeyboardGeneric, automatic, persist);
    }

    private void LoadState()
    {
        var state = _settings.Current.KeyboardFix;
        PrecisionProfile.IsChecked = state.PrecisionProfile;
        MinimumDelay.IsChecked = state.MinimumDelay;
        MaximumRepeat.IsChecked = state.MaximumRepeat;
        FilterKeysOff.IsChecked = state.FilterKeysOff;
        StickyKeysOff.IsChecked = state.StickyKeysOff;
        ToggleKeysOff.IsChecked = state.ToggleKeysOff;
        RegistryVisual.IsChecked = state.RegistryVisual;
        GameModeVisual.IsChecked = state.GameModeVisual;
        AccessibilityVisual.IsChecked = state.AccessibilityVisual;
        FiveMBoostVisual.IsChecked = state.FiveMBoostVisual;
        BackgroundServicesVisual.IsChecked = state.BackgroundServicesVisual;
        UsbSelectiveSuspendVisual.IsChecked = state.UsbSelectiveSuspendVisual;

        var parts = state.SelectedDeviceId.Split('|');
        var known = parts.Length == 2
            ? new KnownDevice(DeviceKind.Keyboard, parts[0], parts[1], "", "", "")
            : KnownDevices.GenericKeyboard;
        SelectMatchingDevice(known, automatic: false, persist: false);
        SelectProfile(_settings.Current.Profiles.KeyboardByDevice.TryGetValue(state.SelectedDeviceId, out var profile) ? profile : _settings.Current.Profiles.KeyboardProfile);
        DetectedKeyboardText.Text = $"Seleção salva: {SelectedKeyboardText.Text}";
    }

    private void SaveState()
    {
        if (!_isReady)
            return;

        var state = _settings.Current.KeyboardFix;
        state.PrecisionProfile = PrecisionProfile.IsChecked == true;
        state.MinimumDelay = MinimumDelay.IsChecked == true;
        state.MaximumRepeat = MaximumRepeat.IsChecked == true;
        state.FilterKeysOff = FilterKeysOff.IsChecked == true;
        state.StickyKeysOff = StickyKeysOff.IsChecked == true;
        state.ToggleKeysOff = ToggleKeysOff.IsChecked == true;
        state.RegistryVisual = RegistryVisual.IsChecked == true;
        state.GameModeVisual = GameModeVisual.IsChecked == true;
        state.AccessibilityVisual = AccessibilityVisual.IsChecked == true;
        state.FiveMBoostVisual = FiveMBoostVisual.IsChecked == true;
        state.BackgroundServicesVisual = BackgroundServicesVisual.IsChecked == true;
        state.UsbSelectiveSuspendVisual = UsbSelectiveSuspendVisual.IsChecked == true;
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
        _settings.Current.Profiles.KeyboardProfile = profile;
        _settings.Current.Profiles.KeyboardByDevice[_settings.Current.KeyboardFix.SelectedDeviceId] = profile;
        _settings.Save();
        var plan = ProfileCatalog.Get(profile);
        OperationText.Text = $"NÃO APLICADO\n{string.Join(" · ", plan.KeyboardChanges.Select(change => change.Title))}\n{plan.KeyboardChanges.Count} alterações planejadas; nenhuma ação oculta.";
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
