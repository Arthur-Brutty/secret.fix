using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Views;

public partial class DisplayView : UserControl
{
    private readonly bool _allowed;
    private readonly PlanTier _minimumPlan;
    private readonly SettingsService _settings;
    private readonly AppLogService _log;
    private bool _isReady;
    private bool _isApplyingPreset;

    public DisplayView(bool allowed, PlanTier minimumPlan, SettingsService settings, AppLogService log)
    {
        _allowed = allowed;
        _minimumPlan = minimumPlan;
        _settings = settings;
        _log = log;
        InitializeComponent();
        LockText.Text = allowed ? "APEX · EXPERIMENTAL" : $"{minimumPlan.ToString().ToUpperInvariant()} ONLY";
        LoadState();
        _isReady = true;
        UpdateValues(save: false);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string label })
            return;

        var preset = label switch
        {
            "NORMAL" => DisplayPreset.Normal,
            "FPS" => DisplayPreset.Fps,
            "VIBRANTE" => DisplayPreset.Vibrant,
            _ => DisplayPreset.Custom
        };

        _isApplyingPreset = true;
        var state = _settings.Current.Display;
        if (preset == DisplayPreset.Custom)
        {
            state.Preset = DisplayPreset.Custom;
        }
        else
        {
            DisplayPresetCatalog.Apply(preset, state);
            SatSlider.Value = state.Saturation;
            TempSlider.Value = state.Temperature;
            GammaSlider.Value = state.Gamma;
        }
        _isApplyingPreset = false;
        UpdateValues(save: true);
        _log.Info($"Display preset selected. Preset={preset}; Saturation={state.Saturation}; Temperature={state.Temperature}; Gamma={state.Gamma}; AppliedToDisplay=false");
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isReady || SatText is null || TempText is null || GammaText is null)
            return;

        if (!_isApplyingPreset)
            _settings.Current.Display.Preset = DisplayPreset.Custom;
        UpdateValues(save: true);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show($"Display requer {_minimumPlan.ToString().ToUpperInvariant()}.");
            return;
        }

        SupportText.Text = "NÃO SUPORTADO NESTE DISPOSITIVO";
        _log.Info($"Display unsupported. Preset={_settings.Current.Display.Preset}; Saturation=false; Temperature=false; Gamma=false; Reason=No safe generic per-monitor API with reliable restore");
        NotificationService.Show("Display não suportado: nenhum ajuste real foi aplicado.");
    }

    private void LoadState()
    {
        var state = _settings.Current.Display;
        SatSlider.Value = state.Saturation;
        TempSlider.Value = state.Temperature;
        GammaSlider.Value = state.Gamma;
        RefreshPresetButtons(state.Preset);
    }

    private void UpdateValues(bool save)
    {
        SatText.Text = $"Saturação: {(int)SatSlider.Value}";
        TempText.Text = $"Temperatura: {(int)TempSlider.Value}";
        GammaText.Text = $"Gamma: {(int)GammaSlider.Value}";

        var state = _settings.Current.Display;
        state.Saturation = SatSlider.Value;
        state.Temperature = TempSlider.Value;
        state.Gamma = GammaSlider.Value;
        RefreshPresetButtons(state.Preset);
        if (save)
            _settings.Save();
    }

    private void RefreshPresetButtons(DisplayPreset selected)
    {
        var buttons = new Dictionary<DisplayPreset, Button>
        {
            [DisplayPreset.Normal] = NormalButton,
            [DisplayPreset.Fps] = FpsButton,
            [DisplayPreset.Vibrant] = VibrantButton,
            [DisplayPreset.Custom] = CustomButton
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
}
