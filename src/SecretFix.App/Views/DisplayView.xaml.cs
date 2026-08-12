using System.Windows;
using System.Windows.Controls;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class DisplayView : UserControl
{
    private bool _isReady;

    public DisplayView(bool allowed, PlanTier minimumPlan)
    {
        InitializeComponent();
        _isReady = true;
        UpdateValues();
        if (!allowed)
            NotificationService.Show($"Display requer {minimumPlan.ToString().ToUpperInvariant()}.");
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            NotificationService.Show($"Preset {button.Content} selecionado no preview.");
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isReady || SatText is null || TempText is null || GammaText is null ||
            SatSlider is null || TempSlider is null || GammaSlider is null)
            return;
        UpdateValues();
    }

    private void UpdateValues()
    {
        SatText.Text = $"Saturação: {(int)SatSlider.Value}";
        TempText.Text = $"Temperatura: {(int)TempSlider.Value}";
        GammaText.Text = $"Gamma: {(int)GammaSlider.Value}";
    }
}
