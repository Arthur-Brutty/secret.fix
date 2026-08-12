namespace SecretFix.Services;

public static class NotificationService
{
    public static event Action<string>? Requested;

    public static void Show(string message) => Requested?.Invoke(message);
}
