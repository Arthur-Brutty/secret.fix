using System.Diagnostics;
using System.IO;

namespace SecretFix.Services;

public sealed record FiveMProcessInfo(int ProcessId, string ProcessName, string? ExecutablePath);

public sealed class FiveMService
{
    private readonly AppLogService _log;

    public FiveMService(AppLogService log)
    {
        _log = log;
    }

    public FiveMProcessInfo? FindRunningProcess()
    {
        foreach (var process in Process.GetProcesses().OrderBy(item => item.ProcessName, StringComparer.OrdinalIgnoreCase))
        {
            using (process)
            {
                if (!process.ProcessName.StartsWith("FiveM", StringComparison.OrdinalIgnoreCase))
                    continue;

                string? path = null;
                try
                {
                    path = process.MainModule?.FileName;
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    _log.Info($"FiveM process path unavailable. PID={process.Id}; Error={ex.Message}");
                }

                return new FiveMProcessInfo(process.Id, process.ProcessName, path);
            }
        }

        return null;
    }

    public string? FindExecutable(string? savedPath)
    {
        var running = FindRunningProcess();
        if (IsValidExecutable(running?.ExecutablePath))
            return Path.GetFullPath(running!.ExecutablePath!);

        foreach (var candidate in StandardCandidates())
        {
            if (IsValidExecutable(candidate))
                return Path.GetFullPath(candidate);
        }

        return IsValidExecutable(savedPath) ? Path.GetFullPath(savedPath!) : null;
    }

    public bool TryStart(string executablePath, out string error)
    {
        error = "";
        if (!IsValidExecutable(executablePath))
        {
            error = "Executável do FiveM inválido ou inexistente.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(executablePath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? ""
            });
            _log.Info($"FiveM started. Path={executablePath}");
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            _log.Error($"FiveM start failed. Path={executablePath}", ex);
            return false;
        }
    }

    public static bool IsValidExecutable(string? path)
        => !string.IsNullOrWhiteSpace(path) &&
           File.Exists(path) &&
           string.Equals(Path.GetFileName(path), "FiveM.exe", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> StandardCandidates()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return new[]
        {
            Path.Combine(local, "FiveM", "FiveM.exe"),
            Path.Combine(local, "FiveM", "FiveM.app", "FiveM.exe"),
            Path.Combine(programFiles, "FiveM", "FiveM.exe"),
            Path.Combine(programFilesX86, "FiveM", "FiveM.exe")
        };
    }
}
