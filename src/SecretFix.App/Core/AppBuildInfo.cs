using System.Reflection;
using System.IO;

namespace SecretFix.Core;

public static class AppBuildInfo
{
    private static readonly Assembly Assembly = typeof(AppBuildInfo).Assembly;

    public static string InformationalVersion =>
        Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "v0.6-test";

    public static string BuildTimestamp
    {
        get
        {
            var path = Assembly.Location;
            return string.IsNullOrWhiteSpace(path) || !File.Exists(path)
                ? "não disponível"
                : File.GetLastWriteTime(path).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
    }

    // A commit is shown only when a build pipeline explicitly embeds one.
    public static string Commit => "não incorporado";
}
