using System.IO;
using System.Text.Json;
using SecretFix.Infrastructure.Windows;

namespace SecretFix.Services;

public sealed class BackupService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecretFix", "backups");

    public string SaveMouse(MouseSnapshot snapshot)
        => Save("mouse", snapshot);

    public string SaveKeyboard(KeyboardSnapshot snapshot)
        => Save("keyboard", snapshot);

    public MouseSnapshot? LoadLatestMouse()
        => LoadLatest<MouseSnapshot>("mouse");

    public KeyboardSnapshot? LoadLatestKeyboard()
        => LoadLatest<KeyboardSnapshot>("keyboard");

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

        var latest = Directory
            .EnumerateFiles(_folder, $"{prefix}-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
            return default;

        return JsonSerializer.Deserialize<T>(File.ReadAllText(latest));
    }
}
