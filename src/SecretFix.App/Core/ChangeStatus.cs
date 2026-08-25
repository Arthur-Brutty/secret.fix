namespace SecretFix.Core;

public enum ChangeStatus
{
    NotApplied,
    Applying,
    Applied,
    Verified,
    Failed,
    NotSupported,
    Restored
}

public static class ChangeStatusText
{
    public static string ToDisplay(this ChangeStatus status) => status switch
    {
        ChangeStatus.NotApplied => "NÃO APLICADO",
        ChangeStatus.Applying => "APLICANDO",
        ChangeStatus.Applied => "APLICADO",
        ChangeStatus.Verified => "VERIFICADO",
        ChangeStatus.Failed => "FALHOU",
        ChangeStatus.NotSupported => "NÃO SUPORTADO",
        ChangeStatus.Restored => "RESTAURADO",
        _ => status.ToString().ToUpperInvariant()
    };
}
