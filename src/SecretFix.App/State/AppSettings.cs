namespace SecretFix.State;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;
    public MouseFixState MouseFix { get; set; } = new();
    public KeyboardFixState KeyboardFix { get; set; } = new();
    public FiveMState FiveM { get; set; } = new();
    public AimState Aim { get; set; } = new();
    public ServicesState Services { get; set; } = new();
    public DisplayState Display { get; set; } = new();
}

public sealed class MouseFixState
{
    public bool MousePrecision { get; set; } = true;
    public bool PerformanceBoost { get; set; } = true;
    public bool Tracking { get; set; } = true;
    public bool Sensitivity { get; set; }
    public bool Flick { get; set; }
    public bool HalfMillisecondExperimental { get; set; }
    public bool RegistryVisual { get; set; }
    public bool IslcVisual { get; set; }
    public bool SensitivityXY { get; set; } = true;
    public bool FlagsVisual { get; set; }
    public bool AccessibilityVisual { get; set; } = true;
    public bool FiveMBoostVisual { get; set; }
    public string SelectedDeviceId { get; set; } = "secret.fix|Generic";
}

public sealed class KeyboardFixState
{
    public bool PrecisionProfile { get; set; } = true;
    public bool MinimumDelay { get; set; } = true;
    public bool MaximumRepeat { get; set; } = true;
    public bool FilterKeysOff { get; set; } = true;
    public bool StickyKeysOff { get; set; } = true;
    public bool ToggleKeysOff { get; set; }
    public bool RegistryVisual { get; set; } = true;
    public bool GameModeVisual { get; set; } = true;
    public bool AccessibilityVisual { get; set; }
    public bool FiveMBoostVisual { get; set; }
    public bool BackgroundServicesVisual { get; set; }
    public bool UsbSelectiveSuspendVisual { get; set; }
    public string SelectedDeviceId { get; set; } = "secret.fix|Generic Keyboard";
}

public sealed class FiveMState
{
    public string? ExecutablePath { get; set; }
}

public enum AimPreset
{
    Basic,
    Medium,
    High,
    Custom
}

public sealed class AimState
{
    public AimPreset Preset { get; set; } = AimPreset.Custom;
    public double Size { get; set; } = 34;
    public double Thickness { get; set; } = 2;
    public double Gap { get; set; } = 8;
    public double Opacity { get; set; } = 90;
    public string Color { get; set; } = "Red";
}

public sealed class ServicesState
{
    public bool BackgroundApps { get; set; }
    public bool GameBar { get; set; }
    public bool PowerPlan { get; set; }
    public bool OptionalServices { get; set; }
}

public enum DisplayPreset
{
    Normal,
    Fps,
    Vibrant,
    Custom
}

public sealed class DisplayState
{
    public DisplayPreset Preset { get; set; } = DisplayPreset.Normal;
    public double Saturation { get; set; } = 50;
    public double Temperature { get; set; } = 50;
    public double Gamma { get; set; } = 50;
}
