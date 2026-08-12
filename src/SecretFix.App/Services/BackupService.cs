using System.Text.Json;
using SecretFix.Infrastructure.Windows;

namespace SecretFix.Services;

public sealed class BackupService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecretFix", "backups");

    public string SaveMouse(MouseSnapshot snapshot)
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, $"mouse-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }
}
