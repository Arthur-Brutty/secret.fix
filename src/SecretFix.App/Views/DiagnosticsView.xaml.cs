using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SecretFix.Core;
using SecretFix.Services;
using SecretFix.State;

namespace SecretFix.Views;

public partial class DiagnosticsView : UserControl, IDisposable
{
    private readonly SettingsService _settings;
    private readonly DiagnosticsService _diagnostics;
    private readonly ProfileOperationService _profiles;
    private readonly BackupService _backup;
    private readonly OperationService _operations;
    private readonly AppLogService _log;
    private readonly PrecisionEngineService _precision = new();
    private readonly InputBenchmarkService _benchmark = new();
    private InputBenchmarkResult? _lastBenchmark;

    public DiagnosticsView(SettingsService settings, BackupService backup, OperationService operations, AppLogService log)
    {
        _settings = settings;
        _backup = backup;
        _operations = operations;
        _log = log;
        _diagnostics = new DiagnosticsService(log, operations);
        _profiles = new ProfileOperationService(backup, operations, log);
        InitializeComponent();
        _benchmark.DeviceChanged += Benchmark_DeviceChanged;
        Loaded += async (_, _) => await RefreshAsync();
        Unloaded += (_, _) => _benchmark.Stop();
    }

    private async Task RefreshAsync()
    {
        var mouseTask = _diagnostics.ReadMouseAsync(_settings.Current.MouseFix.SelectedDeviceId, GetMouseProfile().ToString());
        var keyboardTask = _diagnostics.ReadKeyboardAsync(_settings.Current.KeyboardFix.SelectedDeviceId, GetKeyboardProfile().ToString());
        await Task.WhenAll(mouseTask, keyboardTask);
        var mouse = mouseTask.Result;
        var keyboard = keyboardTask.Result;
        MouseDiagnosticText.Text = $"Nome: {mouse.Device.DisplayName}\nFabricante: {Value(mouse.Device.Manufacturer)} · VID: {Value(mouse.Device.Vid)} · PID: {Value(mouse.Device.Pid)}\nTipo: {mouse.Device.Kind} HID · Modelo: {(mouse.Device.IsExactMatch ? mouse.Device.KnownDevice?.Model : "Generic")}\nSelecionado: {mouse.SelectedDevice}\nWindows: {(mouse.Settings is null ? "Não foi possível verificar" : ProfileOperationService.Describe(mouse.Settings))}\nPerfil: {mouse.Profile} · Última alteração: {mouse.LastChange?.ToLocalTime().ToString("g") ?? "—"}";
        KeyboardDiagnosticText.Text = $"Nome: {keyboard.Device.DisplayName}\nFabricante: {Value(keyboard.Device.Manufacturer)} · VID: {Value(keyboard.Device.Vid)} · PID: {Value(keyboard.Device.Pid)}\nTipo: {keyboard.Device.Kind} HID · Modelo: {(keyboard.Device.IsExactMatch ? keyboard.Device.KnownDevice?.Model : "Generic")}\nSelecionado: {keyboard.SelectedDevice}\nWindows: {(keyboard.Settings is null ? "Não foi possível verificar" : ProfileOperationService.Describe(keyboard.Settings))}\nPerfil: {keyboard.Profile} · Última alteração: {keyboard.LastChange?.ToLocalTime().ToString("g") ?? "—"}";
        UpdateRecovery();
        UpdateHistory();
        UpdateComparison();
        UpdatePrecisionHealth(mouse);
        RejectedTweaksText.Text = string.Join("\n", TweakEvidenceCatalog.All.Where(entry => entry.Evidence == TweakEvidenceLevel.Rejected).Select(entry => $"• {entry.Title}: {entry.Rationale}"));
    }

    private static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "não disponível" : value;

    private OptimizationProfile SelectedProfile => Enum.TryParse<OptimizationProfile>((ProfilePicker.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var profile) ? profile : OptimizationProfile.Balanced;
    private OptimizationProfile GetMouseProfile() => _settings.Current.Profiles.MouseByDevice.TryGetValue(_settings.Current.MouseFix.SelectedDeviceId, out var profile) ? profile : _settings.Current.Profiles.MouseProfile;
    private OptimizationProfile GetKeyboardProfile() => _settings.Current.Profiles.KeyboardByDevice.TryGetValue(_settings.Current.KeyboardFix.SelectedDeviceId, out var profile) ? profile : _settings.Current.Profiles.KeyboardProfile;

    private void ProfilePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilePlanText is null) return;
        var definition = ProfileCatalog.Get(SelectedProfile);
        var changes = definition.MouseChanges.Concat(definition.KeyboardChanges).Select(change => $"• {change.Module}: {change.Title} — {change.Description}");
        ProfilePlanText.Text = $"{definition.Description}\n{string.Join("\n", changes)}\n{definition.MouseChanges.Count + definition.KeyboardChanges.Count} alterações planejadas; nenhuma ação oculta.";
    }

    private async void ApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = SelectedProfile;
        _settings.Current.Profiles.MouseProfile = profile;
        _settings.Current.Profiles.KeyboardProfile = profile;
        _settings.Current.Profiles.MouseByDevice[_settings.Current.MouseFix.SelectedDeviceId] = profile;
        _settings.Current.Profiles.KeyboardByDevice[_settings.Current.KeyboardFix.SelectedDeviceId] = profile;
        _settings.Save();
        ProfileResultText.Text = "APLICANDO — snapshot, backup, alteração e releitura em andamento.";
        var mouse = await Task.Run(() => _profiles.ApplyMouse(profile, _settings.Current.MouseFix));
        var keyboard = await Task.Run(() => _profiles.ApplyKeyboard(profile, _settings.Current.KeyboardFix));
        ProfileResultText.Text = $"MOUSE {mouse.Status.ToDisplay()}\nANTES: {mouse.Before}\nDEPOIS: {mouse.After}\n{mouse.Message}\n\nTECLADO {keyboard.Status.ToDisplay()}\nANTES: {keyboard.Before}\nDEPOIS: {keyboard.After}\n{keyboard.Message}";
        await RefreshAsync();
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var mouse = _backup.LoadLatestMouse();
        var keyboard = _backup.LoadLatestKeyboard();
        var messages = new List<string>();
        if (mouse is not null) messages.Add((await Task.Run(() => _profiles.RestoreMouse(mouse))).Status.ToDisplay());
        if (keyboard is not null) messages.Add((await Task.Run(() => _profiles.RestoreKeyboard(keyboard))).Status.ToDisplay());
        ProfileResultText.Text = messages.Count == 0 ? "Nenhum backup compatível encontrado." : $"Restauração: {string.Join(" / ", messages)}. Estado relido após cada operação.";
        await RefreshAsync();
    }

    private void BenchmarkStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = Window.GetWindow(this) ?? throw new InvalidOperationException("Janela principal indisponível.");
            _benchmark.Start(new WindowInteropHelper(window));
            BenchmarkRange.Text = "Teste em execução. Mova o mouse normalmente; PARAR encerra e libera o hook Raw Input.";
            _log.Info("Input benchmark started.");
        }
        catch (Exception ex) { BenchmarkRange.Text = $"Não foi possível iniciar: {ex.Message}"; _log.Error("Input benchmark start failed", ex); }
    }

    private void BenchmarkStop_Click(object sender, RoutedEventArgs e)
    {
        _lastBenchmark = _benchmark.Stop();
        ShowBenchmark(_lastBenchmark);
        RenderBenchmarkGraph(_lastBenchmark);
        _log.Info($"Input benchmark stopped. Events={_lastBenchmark.EventCount}; Avg={_lastBenchmark.AverageIntervalMs}; Jitter={_lastBenchmark.JitterMs}; Polling={_lastBenchmark.EstimatedPollingHz}");
    }

    private void BenchmarkReset_Click(object sender, RoutedEventArgs e)
    {
        _benchmark.Stop(); _lastBenchmark = null;
        BenchmarkEvents.Text = BenchmarkPolling.Text = BenchmarkAverage.Text = BenchmarkJitter.Text = "—";
        BenchmarkRange.Text = "";
        BenchmarkGraph.Children.Clear();
    }

    private void SaveBefore_Click(object sender, RoutedEventArgs e) => SaveBenchmark(before: true);
    private void SaveAfter_Click(object sender, RoutedEventArgs e) => SaveBenchmark(before: false);
    private void SaveBenchmark(bool before)
    {
        if (_lastBenchmark is null) { BenchmarkRange.Text = "Conclua um teste antes de salvá-lo."; return; }
        var result = new InputBenchmarkState { AnalyzerVersion = InputBenchmarkResult.AnalyzerVersion, Device = _lastBenchmark.DeviceDisplayName, CapturedAt = DateTimeOffset.UtcNow, EventCount = _lastBenchmark.EventCount, DurationMs = _lastBenchmark.Duration.TotalMilliseconds, AverageIntervalMs = _lastBenchmark.AverageIntervalMs, MinimumIntervalMs = _lastBenchmark.MinimumIntervalMs, MaximumIntervalMs = _lastBenchmark.MaximumIntervalMs, JitterMs = _lastBenchmark.JitterMs, EstimatedPollingHz = _lastBenchmark.ObservedEventRateHz, StabilityPercent = _lastBenchmark.StabilityPercent, MedianIntervalMs = _lastBenchmark.MedianIntervalMs, P95IntervalMs = _lastBenchmark.P95IntervalMs, P99IntervalMs = _lastBenchmark.P99IntervalMs, OutlierCount = _lastBenchmark.OutlierCount, LargeGapCount = _lastBenchmark.LargeGapCount, SampleQuality = _lastBenchmark.SampleQuality.ToString().ToUpperInvariant() };
        if (before) _settings.Current.Benchmark.Before = result; else _settings.Current.Benchmark.After = result;
        _settings.Save(); UpdateComparison();
    }

    private void ShowBenchmark(InputBenchmarkResult result)
    {
        BenchmarkEvents.Text = result.EventCount.ToString();
        BenchmarkPolling.Text = result.ObservedEventRateHz is double rate ? $"{rate:F0} Hz" : "dados insuficientes";
        BenchmarkAverage.Text = result.MedianIntervalMs is double median ? $"{median:F2} ms" : "dados insuficientes";
        BenchmarkJitter.Text = result.JitterMs is double jitter ? $"{jitter:F2} ms / {result.StabilityPercent:F0}%" : "dados insuficientes";
        BenchmarkRange.Text = result.EventCount == 0 ? "Nenhum evento de mouse foi recebido." : $"Duração: {result.Duration.TotalSeconds:F1}s · Mín: {result.MinimumIntervalMs:F2} ms · Máx: {result.MaximumIntervalMs:F2} ms. Estimativa baseada nos eventos recebidos pelo Windows.";
    }

    private void UpdateComparison()
    {
        var before = _settings.Current.Benchmark.Before; var after = _settings.Current.Benchmark.After;
        BenchmarkCompareText.Text = before is null && after is null ? "Salve um TESTE ANTES e um TESTE DEPOIS para comparar somente os números." : $"ANTES: {Describe(before)}\nDEPOIS: {Describe(after)}\nA comparação não atribui causalidade à otimização.";
    }
    private static string Describe(InputBenchmarkState? value)
        => value is null
            ? "não registrado"
            : $"{value.EventCount} eventos · {Format(value.AverageIntervalMs, "F2", "dados insuficientes")} ms · ~{Format(value.EstimatedPollingHz, "F0", "—")} Hz · jitter {Format(value.JitterMs, "F2", "dados insuficientes")} ms";

    private static string Format(double? value, string format, string fallback)
        => value is double number ? number.ToString(format) : fallback;

    private void RenderBenchmarkGraph(InputBenchmarkResult result)
    {
        BenchmarkGraph.Children.Clear();
        var values = result.IntervalsMs.TakeLast(120).ToArray();
        if (values.Length < 2)
            return;

        var width = Math.Max(120, BenchmarkGraph.ActualWidth);
        const double height = 86;
        var low = values.Min();
        var high = values.Max();
        var span = Math.Max(0.01, high - low);
        var line = new Polyline { Stroke = (Brush)FindResource("AccentBrush"), StrokeThickness = 1.5, SnapsToDevicePixels = true };
        for (var index = 0; index < values.Length; index++)
        {
            var x = index * width / (values.Length - 1d);
            var y = height - 6 - ((values[index] - low) / span * (height - 12));
            line.Points.Add(new Point(x, y));
        }
        BenchmarkGraph.Children.Add(line);
    }

    private void UpdatePrecisionHealth(MouseDiagnostics mouse)
    {
        var drift = mouse.Settings is null ? null : _precision.GetDrift(GetMouseProfile(), mouse.Settings);
        var last = _settings.Current.Benchmark.After ?? _settings.Current.Benchmark.Before;
        PrecisionHealthText.Text = $"Selected Mouse: {mouse.Device.DisplayName}\nVID/PID: {Value(mouse.Device.Vid)}/{Value(mouse.Device.Pid)} · Raw Input: available during benchmark\nWindows Pointer Speed: {mouse.Settings?.Speed.ToString() ?? "unavailable"} · Acceleration: {(mouse.Settings is null ? "unavailable" : mouse.Settings.Acceleration == 0 ? "OFF" : "ON")}\nCurrent Profile: {GetMouseProfile()} · Drift: {drift?.Message ?? "not available"}\nLast Benchmark: {(last is null ? "none" : $"{last.EventCount} events, {last.EstimatedPollingHz:F0} Hz observed, P95 {last.P95IntervalMs:F2} ms, {last.SampleQuality}")}\nBackup State: {(_backup.LoadLatestMouse() is null ? "No mouse backup found" : "Mouse backup available")}";
    }

    private async void RestoreProfile_Click(object sender, RoutedEventArgs e)
    {
        var operation = await Task.Run(() => _profiles.ApplyMouse(GetMouseProfile(), _settings.Current.MouseFix));
        ProfileResultText.Text = $"Restore profile: {operation.Status.ToDisplay()} — {operation.Message}";
        await RefreshAsync();
    }

    private async void AcceptCurrentState_Click(object sender, RoutedEventArgs e)
    {
        // Accept means that no automatic expectation is enforced until the user chooses a profile again.
        _settings.Current.Profiles.MouseProfile = OptimizationProfile.Custom;
        _settings.Current.Profiles.MouseByDevice[_settings.Current.MouseFix.SelectedDeviceId] = OptimizationProfile.Custom;
        _settings.Save();
        await RefreshAsync();
    }

    private async void ExportBenchmark_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBenchmark is null) { BenchmarkRange.Text = "Complete a benchmark before exporting JSON."; return; }
        var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"secret-fix-benchmark-{DateTime.Now:yyyyMMdd-HHmmss}.json" };
        if (dialog.ShowDialog() != true) return;
        try { await _precision.ExportBenchmarkAsync(_lastBenchmark, dialog.FileName); BenchmarkRange.Text = "Benchmark JSON exported locally."; }
        catch (Exception ex) { BenchmarkRange.Text = $"Export failed: {ex.Message}"; _log.Error("Benchmark export failed", ex); }
    }

    private void Benchmark_DeviceChanged(object? sender, string message) => Dispatcher.BeginInvoke(() => BenchmarkRange.Text = message + " Capture remains safe; no devices were merged.");

    private void UpdateHistory()
    {
        HistoryList.ItemsSource = _operations.LoadHistory().Select(item => $"{item.Timestamp.ToLocalTime():g}  |  {item.Module} {item.Profile}  |  {item.ChangeCount} mudanças  |  {item.Status.ToDisplay()}\n{item.Summary}").ToList();
    }
    private void UpdateRecovery()
    {
        var pending = _operations.GetPending();
        RecoveryCard.Visibility = pending is null ? Visibility.Collapsed : Visibility.Visible;
        if (pending is not null) RecoveryText.Text = $"Uma alteração anterior pode não ter sido concluída: {pending.Module} / {pending.Profile}, iniciada em {pending.StartedAt.ToLocalTime():g}.";
    }
    private async void RecoveryVerify_Click(object sender, RoutedEventArgs e)
    {
        var pending = _operations.GetPending();
        if (pending is null) return;
        await RefreshAsync();
        RecoveryText.Text = pending.Module.Equals("Mouse", StringComparison.OrdinalIgnoreCase)
            ? "Estado atual relido: " + MouseDiagnosticText.Text.Split("Windows: ").LastOrDefault() + "\nA releitura não confirma que a operação interrompida foi concluída; restaure ou ignore somente após revisar o estado."
            : "Estado atual relido: " + KeyboardDiagnosticText.Text.Split("Windows: ").LastOrDefault() + "\nA releitura não confirma que a operação interrompida foi concluída; restaure ou ignore somente após revisar o estado.";
    }
    private async void RecoveryRestore_Click(object sender, RoutedEventArgs e)
    {
        var pending = _operations.GetPending(); if (pending is null) return;
        if (pending.Module.Equals("Mouse", StringComparison.OrdinalIgnoreCase) && _backup.LoadMouse(pending.BackupPath) is { } mouse) await Task.Run(() => _profiles.RestoreMouse(mouse, "recovery"));
        else if (pending.Module.Equals("Keyboard", StringComparison.OrdinalIgnoreCase) && _backup.LoadKeyboard(pending.BackupPath) is { } keyboard) await Task.Run(() => _profiles.RestoreKeyboard(keyboard, "recovery"));
        else RecoveryText.Text = "O backup relacionado não está disponível; nenhuma restauração foi feita.";
        await RefreshAsync();
    }
    private void RecoveryIgnore_Click(object sender, RoutedEventArgs e) { _operations.IgnorePending(); UpdateRecovery(); UpdateHistory(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    public void Dispose() { _benchmark.DeviceChanged -= Benchmark_DeviceChanged; _benchmark.Dispose(); }
}
