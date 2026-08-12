using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SecretFix.Infrastructure.Windows;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class KeyboardFixView : UserControl
{
    private readonly WindowsKeyboardService _keyboard = new();
    private readonly BackupService _backup;
    private readonly AppLogService _log;
    private KeyboardSnapshot? _sessionSnapshot;
    private Button? _selectedDevice;

    public KeyboardFixView(BackupService backup, AppLogService log)
    {
        InitializeComponent();
        _backup = backup;
        _log = log;
        SelectDevice(KeyboardWooting);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var before = _keyboard.ReadKeyboard();
            _sessionSnapshot ??= before;
            var backupPath = _backup.SaveKeyboard(before);

            _keyboard.ApplyGamingProfile(
                disableFilterKeys: FilterKeysOff.IsChecked == true,
                disableStickyKeys: StickyKeysOff.IsChecked == true,
                disableToggleKeys: ToggleKeysOff.IsChecked == true);

            var after = _keyboard.ReadKeyboard();
            _log.Info($"KeyboardFix applied. Before={before}; After={after}; Backup={backupPath}");
            StatusText.Text = $"Aplicado com backup salvo. Antes: speed {before.Speed}, delay {before.Delay}. Depois: speed {after.Speed}, delay {after.Delay}.";
            ShowToast("TecladoFix aplicado com snapshot validado.");
        }
        catch (Exception ex)
        {
            _log.Info($"KeyboardFix apply failed. Error={ex.Message}");
            StatusText.Text = $"Falha ao aplicar: {ex.Message}";
            ShowToast("Falha ao aplicar TecladoFix.");
        }
    }

    private void RestoreSession_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionSnapshot is null)
        {
            StatusText.Text = "Nenhum snapshot desta sessao para restaurar.";
            ShowToast("Nao ha snapshot de sessao.");
            return;
        }

        RestoreSnapshot(_sessionSnapshot, "sessao");
    }

    private void RestoreLatest_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backup.LoadLatestKeyboard();
        if (latest is null)
        {
            StatusText.Text = "Nenhum backup de teclado encontrado em LocalAppData/SecretFix/backups.";
            ShowToast("Nenhum backup encontrado.");
            return;
        }

        RestoreSnapshot(latest, "ultimo backup");
    }

    private void Device_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            SelectDevice(button);
    }

    private void SelectDevice(Button button)
    {
        if (_selectedDevice is not null)
        {
            _selectedDevice.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush");
            _selectedDevice.Background = System.Windows.Media.Brushes.Transparent;
        }

        _selectedDevice = button;
        button.BorderBrush = (System.Windows.Media.Brush)FindResource("AccentBrush");
        button.Background = (System.Windows.Media.Brush)FindResource("DangerWashBrush");
        ShowToast($"{button.Tag} selecionado.");
    }

    private void RestoreSnapshot(KeyboardSnapshot snapshot, string source)
    {
        try
        {
            _keyboard.Restore(snapshot);
            var after = _keyboard.ReadKeyboard();
            _log.Info($"KeyboardFix restored from {source}. Target={snapshot}; After={after}");
            StatusText.Text = $"Restaurado a partir de {source}. Speed atual: {after.Speed}, delay: {after.Delay}.";
            ShowToast("Configuracao do teclado restaurada.");
        }
        catch (Exception ex)
        {
            _log.Info($"KeyboardFix restore failed. Source={source}; Error={ex.Message}");
            StatusText.Text = $"Falha ao restaurar: {ex.Message}";
            ShowToast("Falha ao restaurar teclado.");
        }
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        ToastHost.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            ToastHost.Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }
}
