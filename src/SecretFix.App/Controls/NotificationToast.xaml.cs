using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SecretFix.Controls;

public partial class NotificationToast : UserControl
{
    public event EventHandler? Closed;

    public NotificationToast(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Loaded += (_, _) => AnimateIn();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            AnimateOut();
        };
        timer.Start();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => AnimateOut();

    private void AnimateIn()
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        ToastTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void AnimateOut()
    {
        var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(160));
        fade.Completed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
        BeginAnimation(OpacityProperty, fade);
        ToastTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation(0, 16, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        });
    }
}
