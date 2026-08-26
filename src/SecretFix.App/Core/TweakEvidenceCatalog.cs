namespace SecretFix.Core;

public enum TweakEvidenceLevel { Documented, ReasonableInference, Experimental, Rejected }

public sealed record TweakEvidence(string Id, string Title, TweakEvidenceLevel Evidence, bool SafeForAutomaticProfile, string Rationale, string? RegistryPath = null, string? RegistryValue = null);

public static class TweakEvidenceCatalog
{
    private static readonly IReadOnlyList<TweakEvidence> Entries =
    [
        new("windows-linear-pointer", "Linear Windows Pointer", TweakEvidenceLevel.Documented, true, "Uses documented SystemParametersInfo mouse settings and is reversible."),
        new("keyboard-accessibility", "Keyboard accessibility settings", TweakEvidenceLevel.Documented, true, "Uses documented Windows accessibility settings; this is not a hardware-latency claim."),
        new("timer-lab", "Timer Lab", TweakEvidenceLevel.Experimental, false, "Timer resolution is not the same as mouse latency."),
        new("hid-buffer", "HID buffer experiment", TweakEvidenceLevel.Experimental, false, "Possible to investigate, but lower buffers do not automatically mean lower latency."),
        new("realtime-priority", "Realtime process priority", TweakEvidenceLevel.Rejected, false, "Can starve critical system work and is unsafe."),
        new("bcd-timers", "BCD / HPET timer hacks", TweakEvidenceLevel.Rejected, false, "System-wide changes with no reliable input benefit."),
        new("usb-suspend", "Global USB selective suspend disable", TweakEvidenceLevel.Rejected, false, "Global power change without a proven per-device input benefit."),
        new("polling-booster", "Generic USB polling booster", TweakEvidenceLevel.Rejected, false, "Cannot safely change firmware polling configuration."),
        new("smoothmouse", "Random SmoothMouse curves", TweakEvidenceLevel.Rejected, false, "Obscure settings are not a documented precision improvement."),
        new("islc", "ISLC sold as mouse precision", TweakEvidenceLevel.Rejected, false, "Not a direct, measured mouse precision control."),
        new("fivem-cache-launch", "FiveM cache purge every launch", TweakEvidenceLevel.Rejected, false, "Repair action only; not an input boost."),
        new("security-disable", "Disable Defender, Firewall or Windows Update", TweakEvidenceLevel.Rejected, false, "Reduces security and is unrelated to input precision.")
    ];

    public static IReadOnlyList<TweakEvidence> All => Entries;
    public static IReadOnlyList<TweakEvidence> AutomaticProfileTweaks => Entries.Where(entry => entry.Evidence == TweakEvidenceLevel.Documented && entry.SafeForAutomaticProfile).ToArray();
    public static TweakEvidence? Find(string id) => Entries.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase));
}
