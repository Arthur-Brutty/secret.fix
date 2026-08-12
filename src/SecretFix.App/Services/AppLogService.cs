namespace SecretFix.Services;

public sealed class AppLogService
{
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecretFix", "logs");

    public void Info(string message)
    {
        Directory.CreateDirectory(_folder);
        var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
        File.AppendAllText(Path.Combine(_folder, "secretfix.log"), line);
    }
}
