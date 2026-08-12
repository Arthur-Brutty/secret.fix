using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using SecretFix.Services;

namespace SecretFix;

public partial class CrosshairOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    public CrosshairOverlayWindow()
    {
        InitializeComponent();
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Loaded += (_, _) => ApplyClickThrough();
    }

    public void ApplySettings(CrosshairSettings settings)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;
        var size = settings.Size;
        var gap = settings.Gap;
        var brush = new SolidColorBrush(settings.Color) { Opacity = settings.Opacity };

        ConfigureLine(LeftLine, centerX - gap - size, centerY, centerX - gap, centerY, settings.Thickness, brush);
        ConfigureLine(RightLine, centerX + gap, centerY, centerX + gap + size, centerY, settings.Thickness, brush);
        ConfigureLine(TopLine, centerX, centerY - gap - size, centerX, centerY - gap, settings.Thickness, brush);
        ConfigureLine(BottomLine, centerX, centerY + gap, centerX, centerY + gap + size, settings.Thickness, brush);
    }

    private static void ConfigureLine(Line line, double x1, double y1, double x2, double y2, double thickness, Brush brush)
    {
        line.X1 = x1;
        line.Y1 = y1;
        line.X2 = x2;
        line.Y2 = y2;
        line.StrokeThickness = thickness;
        line.Stroke = brush;
    }

    private void ApplyClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}
