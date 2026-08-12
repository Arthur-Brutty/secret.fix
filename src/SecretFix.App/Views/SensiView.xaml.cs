using System.Windows;
using System.Windows.Controls;
using SecretFix.Core;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class SensiView : UserControl
{
    private readonly WindowsInputService _input = new();
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private readonly bool _allowed;

    public SensiView(BackupService backup, AppLogService log, bool allowed, PlanTier minimumPlan)
    {
        InitializeComponent();
        _backup = backup;
        _log = log;
        _allowed = allowed;
        ReadCurrent();
        if (!allowed)
            NotificationService.Show($"Sensi requer {minimumPlan.ToString().ToUpperInvariant()}+.");
    }

    private void ReadCurrent()
    {
        try
        {
            var snapshot = _input.ReadMouse();
            SpeedSlider.Value = snapshot.Speed;
            CurrentValueText.Text = $"Valor Atual: {snapshot.Speed}";
        }
        catch (Exception ex)
        {
            _log.Info($"Sensi read failed. Error={ex.Message}");
            CurrentValueText.Text = "Valor Atual: indisponível";
        }
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CurrentValueText is not null)
            CurrentValueText.Text = $"Valor Atual: {(int)SpeedSlider.Value}";
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show("Sensi bloqueado para o plano atual.");
            return;
        }

        try
        {
            var before = _input.ReadMouse();
            var backupPath = _backup.SaveMouse(before);
            _input.ApplyLinearMouse((int)SpeedSlider.Value);
            var after = _input.ReadMouse();
            _log.Info($"Sensi applied. Before={before}; After={after}; Backup={backupPath}");
            NotificationService.Show($"Sensi aplicada: valor {after.Speed}.");
        }
        catch (Exception ex)
        {
            _log.Info($"Sensi apply failed. Error={ex.Message}");
            NotificationService.Show($"Falha ao aplicar Sensi: {ex.Message}");
        }
    }

    private void RestoreDefault_Click(object sender, RoutedEventArgs e)
    {
        SpeedSlider.Value = 10;
        Apply_Click(sender, e);
    }
}
