using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Views;

public partial class FiveMView : UserControl
{
    private readonly bool _allowed;
    private readonly PlanTier _minimumPlan;
    private readonly AppLogService _log;
    private readonly SettingsService _settings;
    private readonly FiveMService _fiveM;

    public FiveMView(bool allowed, PlanTier minimumPlan, AppLogService log, SettingsService settings)
    {
        _allowed = allowed;
        _minimumPlan = minimumPlan;
        _log = log;
        _settings = settings;
        _fiveM = new FiveMService(log);
        InitializeComponent();
        PlanText.Text = allowed ? "Disponível (PULSE+)" : $"{minimumPlan.ToString().ToUpperInvariant()}+ ONLY";
        Detect();
    }

    private void Detect()
    {
        var process = _fiveM.FindRunningProcess();
        if (process is not null)
        {
            ProcessText.Text = $"{process.ProcessName} · PID {process.ProcessId}";
            StatusText.Text = "FiveM encontrado em execução.";
            if (FiveMService.IsValidExecutable(process.ExecutablePath))
                SavePath(process.ExecutablePath!);
            PathText.Text = process.ExecutablePath ?? _settings.Current.FiveM.ExecutablePath ?? "Caminho indisponível";
            LocateButton.Content = "ALTERAR LOCAL";
            _log.Info($"FiveM found running. PID={process.ProcessId}; Process={process.ProcessName}; Path={process.ExecutablePath ?? "unavailable"}");
            return;
        }

        ProcessText.Text = "Offline";
        var path = _fiveM.FindExecutable(_settings.Current.FiveM.ExecutablePath);
        if (path is null)
        {
            StatusText.Text = "FiveM não encontrado.";
            PathText.Text = "Não detectado";
            LocateButton.Content = "LOCALIZAR FIVEM";
            _log.Info("FiveM not found in running processes, standard paths, or saved path.");
            return;
        }

        SavePath(path);
        StatusText.Text = "Instalação do FiveM encontrada.";
        PathText.Text = path;
        LocateButton.Content = "ALTERAR LOCAL";
        _log.Info($"FiveM found. Path={path}");
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowed)
        {
            NotificationService.Show($"Jogar FiveM requer {_minimumPlan.ToString().ToUpperInvariant()}+.");
            return;
        }

        var running = _fiveM.FindRunningProcess();
        if (running is not null)
        {
            StatusText.Text = "FiveM já está em execução.";
            ProcessText.Text = $"{running.ProcessName} · PID {running.ProcessId}";
            _log.Info($"FiveM launch skipped because process is already running. PID={running.ProcessId}");
            NotificationService.Show("FiveM já está aberto.");
            return;
        }

        var path = _fiveM.FindExecutable(_settings.Current.FiveM.ExecutablePath);
        if (path is null)
        {
            StatusText.Text = "FiveM não encontrado.";
            LocateButton.Content = "LOCALIZAR FIVEM";
            NotificationService.Show("FiveM não encontrado. Use LOCALIZAR FIVEM.");
            return;
        }

        SavePath(path);
        if (!_fiveM.TryStart(path, out var error))
        {
            StatusText.Text = "Falha ao abrir FiveM.";
            NotificationService.Show($"Falha ao abrir FiveM: {error}");
            return;
        }

        StatusText.Text = "FiveM iniciado. Confirmando processo...";
        NotificationService.Show("FiveM iniciado.");
        await Task.Delay(1200);
        Detect();
    }

    private void Locate_Click(object sender, RoutedEventArgs e)
    {
        var current = _settings.Current.FiveM.ExecutablePath;
        var dialog = new OpenFileDialog
        {
            Title = "Localizar FiveM.exe",
            Filter = "FiveM (FiveM.exe)|FiveM.exe|Executáveis (*.exe)|*.exe",
            FileName = "FiveM.exe",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = FiveMService.IsValidExecutable(current) ? Path.GetDirectoryName(current) : null
        };

        if (dialog.ShowDialog() != true)
            return;

        if (!FiveMService.IsValidExecutable(dialog.FileName))
        {
            _log.Info($"FiveM manual path rejected. FileName={Path.GetFileName(dialog.FileName)}");
            NotificationService.Show("Selecione o executável FiveM.exe.");
            return;
        }

        SavePath(dialog.FileName);
        PathText.Text = dialog.FileName;
        StatusText.Text = "FiveM localizado manualmente.";
        LocateButton.Content = "ALTERAR LOCAL";
        _log.Info($"FiveM located manually. Path={dialog.FileName}");
        NotificationService.Show("Caminho do FiveM salvo.");
    }

    private void SavePath(string path)
    {
        if (string.Equals(_settings.Current.FiveM.ExecutablePath, path, StringComparison.OrdinalIgnoreCase))
            return;

        _settings.Current.FiveM.ExecutablePath = path;
        _settings.Save();
    }

    private void OpenBackups_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecretFix", "backups");
        try
        {
            Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", folder)
            {
                UseShellExecute = true
            });
            _log.Info($"Backup folder opened. Path={folder}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _log.Error($"Backup folder open failed. Path={folder}", ex);
            NotificationService.Show("Não foi possível abrir a pasta de backups.");
        }
    }
}
