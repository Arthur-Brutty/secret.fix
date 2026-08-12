using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class FlickTrainerView : UserControl
{
    private readonly bool _allowed;
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private double _remaining = 60;
    private int _points;
    private int _clicks;

    public FlickTrainerView(bool allowed, PlanTier minimumPlan)
    {
        InitializeComponent();
        _allowed = allowed;
        LockText.Text = allowed ? "" : $"{minimumPlan.ToString().ToUpperInvariant()} ONLY";
        _timer.Tick += (_, _) => Tick();
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show("Flick Trainer requer APEX.");
            return;
        }

        _timer.Start();
        MoveTarget();
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => _timer.Stop();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _remaining = 60;
        _points = 0;
        _clicks = 0;
        UpdateStats();
    }

    private void Target_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_timer.IsEnabled)
            return;

        _points++;
        _clicks++;
        UpdateStats();
        MoveTarget();
        e.Handled = true;
    }

    private void TrainingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_timer.IsEnabled)
        {
            _clicks++;
            UpdateStats();
        }
    }

    private void Tick()
    {
        _remaining = Math.Max(0, _remaining - 0.1);
        if (_remaining <= 0)
            _timer.Stop();
        UpdateStats();
    }

    private void MoveTarget()
    {
        var maxX = Math.Max(40, TrainingCanvas.ActualWidth - Target.Width - 20);
        var maxY = Math.Max(40, TrainingCanvas.ActualHeight - Target.Height - 20);
        Canvas.SetLeft(Target, _random.Next(20, (int)maxX));
        Canvas.SetTop(Target, _random.Next(20, (int)maxY));
    }

    private void UpdateStats()
    {
        PointsText.Text = $"{_points} pontos";
        TimeText.Text = $"{_remaining:0.0}s";
        AccuracyText.Text = _clicks == 0 ? "0%" : $"{(int)Math.Round(_points * 100.0 / _clicks)}%";
    }
}
