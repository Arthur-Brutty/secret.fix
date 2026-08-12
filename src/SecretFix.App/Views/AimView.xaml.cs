using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class AimView : UserControl
{
    public AimView(bool allowed, PlanTier minimumPlan)
    {
        InitializeComponent();
        if (!allowed)
            NotificationService.Show($"Mira requer {minimumPlan.ToString().ToUpperInvariant()}.");
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            NotificationService.Show($"Preset {button.Content} selecionado no preview.");
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LeftLine is null)
            return;

        var size = SizeSlider.Value;
        var gap = GapSlider.Value;
        var thickness = ThicknessSlider.Value;
        var opacity = OpacitySlider.Value / 100;
        ConfigureLine(LeftLine, 125 - gap - size, 125, 125 - gap, 125, thickness, opacity);
        ConfigureLine(RightLine, 125 + gap, 125, 125 + gap + size, 125, thickness, opacity);
        ConfigureLine(TopLine, 125, 125 - gap - size, 125, 125 - gap, thickness, opacity);
        ConfigureLine(BottomLine, 125, 125 + gap, 125, 125 + gap + size, thickness, opacity);
        CrosshairOverlayService.Update(CurrentSettings());
    }

    private void ColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LeftLine is null)
            return;

        CrosshairOverlayService.Update(CurrentSettings());
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        CrosshairOverlayService.Update(CurrentSettings());
        CrosshairOverlayService.Show();
        NotificationService.Show("Mira ativada no centro da tela.");
    }

    private void Deactivate_Click(object sender, RoutedEventArgs e)
    {
        CrosshairOverlayService.Close();
        NotificationService.Show("Mira desativada.");
    }

    private CrosshairSettings CurrentSettings()
    {
        var color = ColorBox?.SelectedIndex == 1 ? Colors.White : Color.FromRgb(217, 31, 47);
        return new CrosshairSettings(SizeSlider.Value, ThicknessSlider.Value, GapSlider.Value, OpacitySlider.Value / 100, color);
    }

    private static void ConfigureLine(Line line, double x1, double y1, double x2, double y2, double thickness, double opacity)
    {
        line.X1 = x1;
        line.Y1 = y1;
        line.X2 = x2;
        line.Y2 = y2;
        line.StrokeThickness = thickness;
        line.Opacity = opacity;
    }
}
