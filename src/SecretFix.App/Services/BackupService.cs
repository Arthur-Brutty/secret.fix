using System.IO;
using System.Text.Json;
using SecretFix.Infrastructure.Windows;

namespace SecretFix.Services;

public sealed class BackupService
{
    private readonly string _folder;
    private readonly AppLogService _log;

    public BackupService(AppLogService? log = null, string? folder = null)
    {
        _log = log ?? new AppLogService();
        _folder = folder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecretFix", "backups");
    }

    public string SaveMouse(MouseSnapshot snapshot)
        => Save("mouse", snapshot);

    public string SaveKeyboard(KeyboardSnapshot snapshot)
        => Save("keyboard", snapshot);

    public MouseSnapshot? LoadLatestMouse()
        => LoadLatest<MouseSnapshot>("mouse");

    public KeyboardSnapshot? LoadLatestKeyboard()
        => LoadLatest<KeyboardSnapshot>("keyboard");

    public MouseSnapshot? LoadMouse(string path) => Load<MouseSnapshot>(path);

    public KeyboardSnapshot? LoadKeyboard(string path) => Load<KeyboardSnapshot>(path);

    private string Save<T>(string prefix, T snapshot)
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private T? LoadLatest<T>(string prefix)
    {
        if (!Directory.Exists(_folder))
            return default;

        var candidates = Directory
            .EnumerateFiles(_folder, $"{prefix}-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc);

        foreach (var candidate in candidates)
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(File.ReadAllText(candidate));
                if (value is not null)
                    return value;

                _log.Info($"Backup ignored because it was empty or invalid. Path={candidate}");
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                _log.Error($"Backup ignored because it could not be read. Path={candidate}", ex);
            }
        }

        return default;
    }

    private T? Load<T>(string path)
    {
        try
        {
            if (!File.Exists(path)) return default;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _log.Error($"Backup could not be read. Path={path}", ex);
            return default;
        }
    }
}
