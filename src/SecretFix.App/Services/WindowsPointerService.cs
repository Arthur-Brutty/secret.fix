using SecretFix.Infrastructure.Windows;

namespace SecretFix.Services;

/// <summary>Named precision boundary for documented Windows pointer APIs.</summary>
public sealed class WindowsPointerService
{
    private readonly WindowsInputService _windows = new();
    public MouseSnapshot Read() => _windows.ReadMouse();
    public void ApplyLinear(int speed = 10) => _windows.ApplyLinearMouse(speed);
    public void Restore(MouseSnapshot snapshot) => _windows.Restore(snapshot);
}
