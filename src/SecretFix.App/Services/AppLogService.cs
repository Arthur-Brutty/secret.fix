using System.IO;
using System.Diagnostics;

namespace SecretFix.Services;

public sealed class AppLogService
{
    private static readonly object Sync = new();
    private readonly string _folder;

    public AppLogService(string? folder = null)
    {
        _folder = folder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecretFix", "logs");
    }

    public void Info(string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(_folder);
                File.AppendAllText(Path.Combine(_folder, "secretfix.log"), line);
            }
        }
        catch (Exception primaryException)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(_folder);
                    File.AppendAllText(Path.Combine(_folder, $"secretfix-{DateTime.UtcNow:yyyyMMdd}.log"),
                        $"{line.TrimEnd()} PrimaryLogError={primaryException.Message}{Environment.NewLine}");
                }
            }
            catch (Exception fallbackException)
            {
                Debug.WriteLine($"SecretFix logging failed. Primary={primaryException}; Fallback={fallbackException}");
            }
        }
    }

    public void Error(string message, Exception exception)
    {
        Info($"{message}. {exception}");
    }
}
