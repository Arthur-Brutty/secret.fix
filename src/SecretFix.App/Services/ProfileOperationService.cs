using SecretFix.Core;
using SecretFix.Infrastructure.Windows;
using SecretFix.State;

namespace SecretFix.Services;

public sealed record ProfileOperationResult(ChangeStatus Status, string Before, string After, string? BackupPath, int ChangeCount, string Message)
{
    public bool IsVerified => Status == ChangeStatus.Verified;
}

public sealed class ProfileOperationService
{
    private readonly WindowsInputService _mouse = new();
    private readonly WindowsKeyboardService _keyboard = new();
    private readonly BackupService _backup;
    private readonly OperationService _operations;
    private readonly AppLogService _log;

    public ProfileOperationService(BackupService backup, OperationService operations, AppLogService log)
    {
        _backup = backup;
        _operations = operations;
        _log = log;
    }

    public ProfileOperationResult ApplyMouse(OptimizationProfile profile, MouseFixState custom)
    {
        MouseSnapshot before;
        try { before = _mouse.ReadMouse(); }
        catch (Exception ex) { return Failed("Mouse", profile, ex.Message); }

        string backup;
        PendingOperation pending;
        try
        {
            backup = _backup.SaveMouse(before);
            pending = _operations.Begin("Mouse", profile.ToString(), backup);
        }
        catch (Exception ex)
        {
            _log.Error("Mouse profile backup or operation journal failed", ex);
            return Failed("Mouse", profile, $"Não foi possível criar backup/jornal da operação: {ex.Message}");
        }
        try
        {
            var apply = profile != OptimizationProfile.Custom || custom.MousePrecision;
            if (!apply)
            {
                _operations.Complete(pending, ChangeStatus.NotApplied, 0, "Custom profile has no selected Windows mouse change.");
                return new(ChangeStatus.NotApplied, Describe(before), Describe(before), backup, 0, "Nenhuma alteração de mouse selecionada.");
            }

            _mouse.ApplyLinearMouse(profile == OptimizationProfile.Competitive ? 10 : before.Speed);
            var after = _mouse.ReadMouse();
            var verified = after.Acceleration == 0 && after.Threshold1 == 0 && after.Threshold2 == 0 && (profile != OptimizationProfile.Competitive || after.Speed == 10);
            var status = verified ? ChangeStatus.Verified : ChangeStatus.Applied;
            var message = verified ? "Estado relido e confirmado." : "Alteração aplicada, mas não foi possível confirmar todos os valores.";
            _operations.Complete(pending, status, 3, message);
            _log.Info($"Mouse profile applied. Profile={profile}; Before={before}; After={after}; Verified={verified}");
            return new(status, Describe(before), Describe(after), backup, 3, message);
        }
        catch (Exception ex)
        {
            _operations.Complete(pending, ChangeStatus.Failed, 0, ex.Message);
            _log.Error("Mouse profile failed", ex);
            return new(ChangeStatus.Failed, Describe(before), "Não foi possível reler", backup, 0, ex.Message);
        }
    }

    public ProfileOperationResult ApplyKeyboard(OptimizationProfile profile, KeyboardFixState custom)
    {
        KeyboardSnapshot before;
        try { before = _keyboard.ReadKeyboard(); }
        catch (Exception ex) { return Failed("Keyboard", profile, ex.Message); }

        string backup;
        PendingOperation pending;
        try
        {
            backup = _backup.SaveKeyboard(before);
            pending = _operations.Begin("Keyboard", profile.ToString(), backup);
        }
        catch (Exception ex)
        {
            _log.Error("Keyboard profile backup or operation journal failed", ex);
            return Failed("Keyboard", profile, $"Não foi possível criar backup/jornal da operação: {ex.Message}");
        }
        try
        {
            var minimumDelay = profile != OptimizationProfile.Custom || custom.MinimumDelay;
            var maximumRepeat = profile == OptimizationProfile.Competitive || (profile == OptimizationProfile.Custom && custom.MaximumRepeat);
            var filter = profile != OptimizationProfile.Custom || custom.FilterKeysOff;
            var sticky = profile != OptimizationProfile.Custom || custom.StickyKeysOff;
            var toggle = profile == OptimizationProfile.Competitive || (profile == OptimizationProfile.Custom && custom.ToggleKeysOff);
            _keyboard.ApplyGamingProfile(minimumDelay, maximumRepeat, filter, sticky, toggle);
            var after = _keyboard.ReadKeyboard();
            var verified = (!minimumDelay || after.Delay == 0) && (!maximumRepeat || after.Speed == 31) && (!filter || !after.FilterKeysEnabled) && (!sticky || !after.StickyKeysEnabled) && (!toggle || !after.ToggleKeysEnabled);
            var status = verified ? ChangeStatus.Verified : ChangeStatus.Applied;
            var message = verified ? "Estado relido e confirmado." : "Alteração aplicada, mas não foi possível confirmar todos os valores.";
            var count = new[] { minimumDelay, maximumRepeat, filter, sticky, toggle }.Count(value => value);
            _operations.Complete(pending, status, count, message);
            _log.Info($"Keyboard profile applied. Profile={profile}; Before={before}; After={after}; Verified={verified}");
            return new(status, Describe(before), Describe(after), backup, count, message);
        }
        catch (Exception ex)
        {
            _operations.Complete(pending, ChangeStatus.Failed, 0, ex.Message);
            _log.Error("Keyboard profile failed", ex);
            return new(ChangeStatus.Failed, Describe(before), "Não foi possível reler", backup, 0, ex.Message);
        }
    }

    public ProfileOperationResult RestoreMouse(MouseSnapshot snapshot, string source = "backup")
    {
        string backup;
        PendingOperation pending;
        try
        {
            backup = _backup.SaveMouse(_mouse.ReadMouse());
            pending = _operations.Begin("Mouse", "Restore", backup);
        }
        catch (Exception ex)
        {
            _log.Error("Mouse restore backup or operation journal failed", ex);
            return new(ChangeStatus.Failed, "Backup selecionado", "Não foi possível reler", null, 0, $"Não foi possível criar backup/jornal da restauração: {ex.Message}");
        }
        try
        {
            _mouse.Restore(snapshot);
            var after = _mouse.ReadMouse();
            var verified = after == snapshot;
            var status = verified ? ChangeStatus.Restored : ChangeStatus.Applied;
            _operations.Complete(pending, status, 1, $"Restore from {source}. Verified={verified}");
            return new(status, "Backup selecionado", Describe(after), backup, 1, verified ? "Estado restaurado e confirmado." : "Restauração aplicada, mas não foi possível confirmar.");
        }
        catch (Exception ex) { _operations.Complete(pending, ChangeStatus.Failed, 0, ex.Message); return new(ChangeStatus.Failed, "Backup selecionado", "Não foi possível reler", backup, 0, ex.Message); }
    }

    public ProfileOperationResult RestoreKeyboard(KeyboardSnapshot snapshot, string source = "backup")
    {
        string backup;
        PendingOperation pending;
        try
        {
            backup = _backup.SaveKeyboard(_keyboard.ReadKeyboard());
            pending = _operations.Begin("Keyboard", "Restore", backup);
        }
        catch (Exception ex)
        {
            _log.Error("Keyboard restore backup or operation journal failed", ex);
            return new(ChangeStatus.Failed, "Backup selecionado", "Não foi possível reler", null, 0, $"Não foi possível criar backup/jornal da restauração: {ex.Message}");
        }
        try
        {
            _keyboard.Restore(snapshot);
            var after = _keyboard.ReadKeyboard();
            var verified = after == snapshot;
            var status = verified ? ChangeStatus.Restored : ChangeStatus.Applied;
            _operations.Complete(pending, status, 1, $"Restore from {source}. Verified={verified}");
            return new(status, "Backup selecionado", Describe(after), backup, 1, verified ? "Estado restaurado e confirmado." : "Restauração aplicada, mas não foi possível confirmar.");
        }
        catch (Exception ex) { _operations.Complete(pending, ChangeStatus.Failed, 0, ex.Message); return new(ChangeStatus.Failed, "Backup selecionado", "Não foi possível reler", backup, 0, ex.Message); }
    }

    private ProfileOperationResult Failed(string module, OptimizationProfile profile, string message)
    {
        try { _operations.AddHistory(new(DateTimeOffset.UtcNow, module, profile.ToString(), 0, ChangeStatus.Failed, null, message)); }
        catch (Exception ex) { _log.Error("Failed operation could not be added to history", ex); }
        return new(ChangeStatus.Failed, "Não foi possível ler", "Não foi possível reler", null, 0, message);
    }

    public static string Describe(MouseSnapshot value) => $"Aceleração: {(value.Acceleration == 0 ? "OFF" : "ON")} | Velocidade: {value.Speed} | Thresholds: {value.Threshold1}/{value.Threshold2}";
    public static string Describe(KeyboardSnapshot value) => $"Repeat: {value.Speed} | Delay: {value.Delay} | Filter: {(value.FilterKeysEnabled ? "ON" : "OFF")} | Sticky: {(value.StickyKeysEnabled ? "ON" : "OFF")} | Toggle: {(value.ToggleKeysEnabled ? "ON" : "OFF")}";
}
