using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class FiveMView : UserControl
{
    private readonly bool _allowed;
    private readonly AppLogService _log;

    public FiveMView(bool allowed, PlanTier minimumPlan, AppLogService log)
    {
        InitializeComponent();
        _allowed = allowed;
        _log = log;
        PlanText.Text = allowed ? "Disponível" : $"{minimumPlan.ToString().ToUpperInvariant()}+";
        Detect();
    }

    private void Detect()
    {
        var process = Process.GetProcessesByName("FiveM").FirstOrDefault();
        ProcessText.Text = process is null ? "Offline" : $"PID {process.Id}";
        StatusText.Text = process is null ? "FiveM não está em execução." : "FiveM encontrado em execução.";
        _log.Info($"FiveM detection. Found={process is not null}");
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show("FiveM requer PULSE+.");
            return;
        }

        NotificationService.Show("Launcher FiveM preparado. Nenhuma injeção é usada.");
    }
}
