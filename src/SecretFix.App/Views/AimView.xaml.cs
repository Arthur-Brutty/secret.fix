using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Views;

public partial class AimView : UserControl
{
    private readonly bool _allowed;
    private readonly PlanTier _minimumPlan;
    private readonly SettingsService _settings;
    private readonly AppLogService _log;
    private bool _isReady;
    private bool _isApplyingPreset;

    public AimView(bool allowed, PlanTier minimumPlan, SettingsService settings, AppLogService log)
    {
        _allowed = allowed;
        _minimumPlan = minimumPlan;
        _settings = settings;
        _log = log;
        InitializeComponent();
        LockText.Text = allowed ? "APEX" : $"{minimumPlan.ToString().ToUpperInvariant()} ONLY";
        LoadState();
        _isReady = true;
        UpdatePreviewAndOverlay(save: false);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string value } ||
            !Enum.TryParse<AimPreset>(value, true, out var preset))
            return;

        _isApplyingPreset = true;
        var state = _settings.Current.Aim;
        if (preset == AimPreset.Custom)
        {
            state.Preset = AimPreset.Custom;
        }
        else
        {
            AimPresetCatalog.Apply(preset, state);
            SizeSlider.Value = state.Size;
            ThicknessSlider.Value = state.Thickness;
            GapSlider.Value = state.Gap;
            OpacitySlider.Value = state.Opacity;
            ColorBox.SelectedIndex = string.Equals(state.Color, "White", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
        _isApplyingPreset = false;

        UpdatePreviewAndOverlay(save: true);
        _log.Info($"Aim preset selected. Preset={preset}; Size={state.Size}; Thickness={state.Thickness}; Gap={state.Gap}; Opacity={state.Opacity}; Color={state.Color}; OverlayActive={CrosshairOverlayService.IsActive}");
        NotificationService.Show($"Preset de mira {value} aplicado.");
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isReady || LeftLine is null)
            return;

        if (!_isApplyingPreset)
            _settings.Current.Aim.Preset = AimPreset.Custom;
        UpdatePreviewAndOverlay(save: true);
    }

    private void ColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady || LeftLine is null)
            return;

        if (!_isApplyingPreset)
            _settings.Current.Aim.Preset = AimPreset.Custom;
        UpdatePreviewAndOverlay(save: true);
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show($"Ativar Mira requer {_minimumPlan.ToString().ToUpperInvariant()}.");
            return;
        }

        CrosshairOverlayService.Update(CurrentSettings());
        CrosshairOverlayService.Show();
        OverlayStatusText.Text = "Overlay ativo";
        _log.Info($"Aim overlay activated. Preset={_settings.Current.Aim.Preset}");
        NotificationService.Show("Mira ativada no centro da tela.");
    }

    private void Deactivate_Click(object sender, RoutedEventArgs e)
    {
        CrosshairOverlayService.Close();
        OverlayStatusText.Text = "Overlay inativo";
        _log.Info("Aim overlay deactivated.");
        NotificationService.Show("Mira desativada.");
    }

    private void LoadState()
    {
        var state = _settings.Current.Aim;
        SizeSlider.Value = state.Size;
        ThicknessSlider.Value = state.Thickness;
        GapSlider.Value = state.Gap;
        OpacitySlider.Value = state.Opacity;
        ColorBox.SelectedIndex = string.Equals(state.Color, "White", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        RefreshPresetButtons(state.Preset);
        OverlayStatusText.Text = CrosshairOverlayService.IsActive ? "Overlay ativo" : "Overlay inativo";
    }

    private void UpdatePreviewAndOverlay(bool save)
    {
        var size = SizeSlider.Value;
        var gap = GapSlider.Value;
        var thickness = ThicknessSlider.Value;
        var opacity = OpacitySlider.Value / 100;
        var color = SelectedColor();
        ConfigureLine(LeftLine, 125 - gap - size, 125, 125 - gap, 125, thickness, opacity, color);
        ConfigureLine(RightLine, 125 + gap, 125, 125 + gap + size, 125, thickness, opacity, color);
        ConfigureLine(TopLine, 125, 125 - gap - size, 125, 125 - gap, thickness, opacity, color);
        ConfigureLine(BottomLine, 125, 125 + gap, 125, 125 + gap + size, thickness, opacity, color);

        var state = _settings.Current.Aim;
        state.Size = size;
        state.Thickness = thickness;
        state.Gap = gap;
        state.Opacity = OpacitySlider.Value;
        state.Color = ColorBox.SelectedIndex == 1 ? "White" : "Red";
        RefreshPresetButtons(state.Preset);

        CrosshairOverlayService.Update(CurrentSettings());
        if (save)
            _settings.Save();
    }

    private void RefreshPresetButtons(AimPreset selected)
    {
        var buttons = new Dictionary<AimPreset, Button>
        {
            [AimPreset.Basic] = BasicButton,
            [AimPreset.Medium] = MediumButton,
            [AimPreset.High] = HighButton,
            [AimPreset.Custom] = CustomButton
        };

        foreach (var pair in buttons)
        {
            pair.Value.Background = pair.Key == selected
                ? (Brush)FindResource("DangerWashBrush")
                : Brushes.Transparent;
            pair.Value.BorderBrush = pair.Key == selected
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("BorderBrush");
        }
    }

    private CrosshairSettings CurrentSettings()
        => new(SizeSlider.Value, ThicknessSlider.Value, GapSlider.Value, OpacitySlider.Value / 100, SelectedColor());

    private Color SelectedColor()
        => ColorBox.SelectedIndex == 1 ? Colors.White : Color.FromRgb(217, 31, 47);

    private static void ConfigureLine(Line line, double x1, double y1, double x2, double y2, double thickness, double opacity, Color color)
    {
        line.X1 = x1;
        line.Y1 = y1;
        line.X2 = x2;
        line.Y2 = y2;
        line.StrokeThickness = thickness;
        line.Opacity = opacity;
        line.Stroke = new SolidColorBrush(color);
    }
}
