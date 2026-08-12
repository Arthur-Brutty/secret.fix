using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class MouseFixView : UserControl
{
    private readonly WindowsInputService _input = new();
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private MouseSnapshot? _sessionSnapshot;
    private Button? _selectedDevice;

    public MouseFixView(BackupService backup, AppLogService log)
    {
        InitializeComponent();
        _backup = backup;
        _log = log;
        BuildDeviceCards();
        SelectDevice(MouseLogitech);
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
                                Height = 86,
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

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var before = _input.ReadMouse();
            _sessionSnapshot ??= before;
            var backupPath = _backup.SaveMouse(before);

            if (MousePrecision.IsChecked == true)
                _input.ApplyLinearMouse(10);

            var after = _input.ReadMouse();
            _log.Info($"MouseFix applied. Before={before}; After={after}; Backup={backupPath}");
            NotificationService.Show($"MouseFix aplicado. Speed {after.Speed}, accel {after.Acceleration}.");
        }
        catch (Exception ex)
        {
            _log.Info($"MouseFix apply failed. Error={ex.Message}");
            NotificationService.Show($"Falha ao aplicar MouseFix: {ex.Message}");
        }
    }

    private void RestoreLatest_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backup.LoadLatestMouse();
        if (latest is null)
        {
            NotificationService.Show("Nenhum backup de mouse encontrado.");
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
            HeroMouseImage.Source = new BitmapImage(new Uri(parts[0], UriKind.Relative));
            SelectedMouseText.Text = $"{parts[1]} {parts[2]}";
            NotificationService.Show($"{parts[1]} {parts[2]} selecionado");
        }
    }

    private void RestoreSnapshot(MouseSnapshot snapshot, string source)
    {
        try
        {
            _input.Restore(snapshot);
            var after = _input.ReadMouse();
            _log.Info($"MouseFix restored from {source}. Target={snapshot}; After={after}");
            NotificationService.Show($"Mouse restaurado: speed {after.Speed}, accel {after.Acceleration}.");
        }
        catch (Exception ex)
        {
            _log.Info($"MouseFix restore failed. Source={source}; Error={ex.Message}");
            NotificationService.Show($"Falha ao restaurar mouse: {ex.Message}");
        }
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
