using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SecretFix.Core;

namespace SecretFix.Services;

public sealed record PendingOperation(string Id, string Module, string Profile, string BackupPath, DateTimeOffset StartedAt);
public sealed record HistoryEntry(DateTimeOffset Timestamp, string Module, string Profile, int ChangeCount, ChangeStatus Status, string? BackupPath, string Summary);

public sealed class OperationService
{
    private readonly string _folder;
    private readonly string _pendingPath;
    private readonly string _historyPath;
    private readonly AppLogService _log;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public OperationService(AppLogService? log = null, string? folder = null)
    {
        _log = log ?? new AppLogService();
        _folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecretFix");
        _pendingPath = Path.Combine(_folder, "pending-operation.json");
        _historyPath = Path.Combine(_folder, "history.json");
    }

    public PendingOperation Begin(string module, string profile, string backupPath)
    {
        Directory.CreateDirectory(_folder);
        var operation = new PendingOperation(Guid.NewGuid().ToString("N"), module, profile, backupPath, DateTimeOffset.UtcNow);
        WriteAtomic(_pendingPath, operation);
        _log.Info($"Operation started. Id={operation.Id}; Module={module}; Profile={profile}; Backup={backupPath}");
        return operation;
    }

    public PendingOperation? GetPending()
    {
        if (!File.Exists(_pendingPath)) return null;
        try { return JsonSerializer.Deserialize<PendingOperation>(File.ReadAllText(_pendingPath), _json); }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _log.Error("Pending operation could not be read", ex);
            return null;
        }
    }

    public void Complete(PendingOperation operation, ChangeStatus status, int changes, string summary)
    {
        AddHistory(new HistoryEntry(DateTimeOffset.UtcNow, operation.Module, operation.Profile, changes, status, operation.BackupPath, summary));
        try
        {
            if (File.Exists(_pendingPath)) File.Delete(_pendingPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _log.Error("Pending operation cleanup failed", ex); }
        _log.Info($"Operation completed. Id={operation.Id}; Status={status}; Changes={changes}; {summary}");
    }

    public void IgnorePending(string summary = "Pending operation dismissed by user.")
    {
        var pending = GetPending();
        if (pending is null) return;
        Complete(pending, ChangeStatus.NotApplied, 0, summary);
    }

    public void AddHistory(HistoryEntry entry)
    {
        Directory.CreateDirectory(_folder);
        var items = LoadHistory().Take(99).ToList();
        items.Insert(0, entry);
        WriteAtomic(_historyPath, items);
    }

    public IReadOnlyList<HistoryEntry> LoadHistory()
    {
        if (!File.Exists(_historyPath)) return [];
        try { return JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(_historyPath), _json) ?? []; }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _log.Error("History could not be read", ex);
            return [];
        }
    }

    private void WriteAtomic<T>(string path, T value)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, _json));
        File.Move(temporary, path, true);
    }
}
