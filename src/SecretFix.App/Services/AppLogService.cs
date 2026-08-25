using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SecretFix.Services;

public sealed class AppLogService
{
    private static readonly object Sync = new();
    private static readonly (Regex Pattern, string Replacement)[] SensitiveValuePatterns =
    [
        (new Regex(@"(?i)\b(bearer\s+)[^\s;]+"), "$1[REDACTED]"),
        (new Regex(@"(?i)\b(password|pwd|secret|token|api[_-]?key|authorization|license(?:key)?)\s*([=:])\s*[^\s,;]+"), "$1$2[REDACTED]"),
        (new Regex(@"\bSF-[A-Za-z0-9-]+\b"), "SF-****")
    ];
    private readonly string _folder;

    public AppLogService(string? folder = null)
    {
        _folder = folder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecretFix", "logs");
    }

    public void Info(string message)
    {
        var line = $"{DateTimeOffset.Now:O} {Redact(message)}{Environment.NewLine}";
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
        // Exception.ToString() can include request headers or configuration values from a future
        // network integration. Keep local diagnostics useful without writing those values to disk.
        Info($"{message}. ExceptionType={exception.GetType().Name}; Detail={exception.Message}");
    }

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return SensitiveValuePatterns.Aggregate(value, static (current, rule) => rule.Pattern.Replace(current, rule.Replacement));
    }
}
