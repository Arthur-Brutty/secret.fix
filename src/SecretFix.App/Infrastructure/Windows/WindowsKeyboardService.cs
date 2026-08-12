using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SecretFix.Infrastructure.Windows;

public sealed class WindowsKeyboardService
{
    private const uint SPI_GETKEYBOARDSPEED = 0x000A;
    private const uint SPI_SETKEYBOARDSPEED = 0x000B;
    private const uint SPI_GETKEYBOARDDELAY = 0x0016;
    private const uint SPI_SETKEYBOARDDELAY = 0x0017;
    private const uint SPI_GETSTICKYKEYS = 0x003A;
    private const uint SPI_SETSTICKYKEYS = 0x003B;
    private const uint SPI_GETTOGGLEKEYS = 0x0034;
    private const uint SPI_SETTOGGLEKEYS = 0x0035;
    private const uint SPI_GETFILTERKEYS = 0x0032;
    private const uint SPI_SETFILTERKEYS = 0x0033;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;
    private const uint Persist = SPIF_UPDATEINIFILE | SPIF_SENDCHANGE;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref int value, uint flags);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref StickyKeys value, uint flags);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref ToggleKeys value, uint flags);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref FilterKeys value, uint flags);

    public KeyboardSnapshot ReadKeyboard()
    {
        var speed = 0;
        var delay = 0;
        var sticky = new StickyKeys { Size = Marshal.SizeOf<StickyKeys>() };
        var toggle = new ToggleKeys { Size = Marshal.SizeOf<ToggleKeys>() };
        var filter = new FilterKeys { Size = Marshal.SizeOf<FilterKeys>() };

        if (!SystemParametersInfo(SPI_GETKEYBOARDSPEED, 0, ref speed, 0)) ThrowLastError();
        if (!SystemParametersInfo(SPI_GETKEYBOARDDELAY, 0, ref delay, 0)) ThrowLastError();
        if (!SystemParametersInfo(SPI_GETSTICKYKEYS, (uint)sticky.Size, ref sticky, 0)) ThrowLastError();
        if (!SystemParametersInfo(SPI_GETTOGGLEKEYS, (uint)toggle.Size, ref toggle, 0)) ThrowLastError();
        if (!SystemParametersInfo(SPI_GETFILTERKEYS, (uint)filter.Size, ref filter, 0)) ThrowLastError();

        return new KeyboardSnapshot(speed, delay, sticky.Flags, toggle.Flags, filter.Flags, filter.WaitMs, filter.DelayMs, filter.RepeatMs, filter.BounceMs);
    }

    public void ApplyGamingProfile(bool disableFilterKeys, bool disableStickyKeys, bool disableToggleKeys)
    {
        SetSpeed(31);
        SetDelay(0);
        if (disableStickyKeys)
            SetStickyKeys(false);
        if (disableToggleKeys)
            SetToggleKeys(false);
        if (disableFilterKeys)
            SetFilterKeys(false);
    }

    public void Restore(KeyboardSnapshot snapshot)
    {
        SetSpeed(snapshot.Speed);
        SetDelay(snapshot.Delay);

        var sticky = new StickyKeys { Size = Marshal.SizeOf<StickyKeys>(), Flags = snapshot.StickyFlags };
        var toggle = new ToggleKeys { Size = Marshal.SizeOf<ToggleKeys>(), Flags = snapshot.ToggleFlags };
        var filter = new FilterKeys
        {
            Size = Marshal.SizeOf<FilterKeys>(),
            Flags = snapshot.FilterFlags,
            WaitMs = snapshot.FilterWaitMs,
            DelayMs = snapshot.FilterDelayMs,
            RepeatMs = snapshot.FilterRepeatMs,
            BounceMs = snapshot.FilterBounceMs
        };

        if (!SystemParametersInfo(SPI_SETSTICKYKEYS, (uint)sticky.Size, ref sticky, Persist)) ThrowLastError();
        if (!SystemParametersInfo(SPI_SETTOGGLEKEYS, (uint)toggle.Size, ref toggle, Persist)) ThrowLastError();
        if (!SystemParametersInfo(SPI_SETFILTERKEYS, (uint)filter.Size, ref filter, Persist)) ThrowLastError();
    }

    private static void SetSpeed(int speed)
    {
        var value = Math.Clamp(speed, 0, 31);
        if (!SystemParametersInfo(SPI_SETKEYBOARDSPEED, (uint)value, ref value, Persist)) ThrowLastError();
    }

    private static void SetDelay(int delay)
    {
        var value = Math.Clamp(delay, 0, 3);
        if (!SystemParametersInfo(SPI_SETKEYBOARDDELAY, (uint)value, ref value, Persist)) ThrowLastError();
    }

    private static void SetStickyKeys(bool enabled)
    {
        var value = new StickyKeys { Size = Marshal.SizeOf<StickyKeys>(), Flags = enabled ? 1 : 0 };
        if (!SystemParametersInfo(SPI_SETSTICKYKEYS, (uint)value.Size, ref value, Persist)) ThrowLastError();
    }

    private static void SetToggleKeys(bool enabled)
    {
        var value = new ToggleKeys { Size = Marshal.SizeOf<ToggleKeys>(), Flags = enabled ? 1 : 0 };
        if (!SystemParametersInfo(SPI_SETTOGGLEKEYS, (uint)value.Size, ref value, Persist)) ThrowLastError();
    }

    private static void SetFilterKeys(bool enabled)
    {
        var value = new FilterKeys { Size = Marshal.SizeOf<FilterKeys>(), Flags = enabled ? 1 : 0 };
        if (!SystemParametersInfo(SPI_SETFILTERKEYS, (uint)value.Size, ref value, Persist)) ThrowLastError();
    }

    private static void ThrowLastError() => throw new Win32Exception(Marshal.GetLastWin32Error());

    [StructLayout(LayoutKind.Sequential)]
    private struct StickyKeys
    {
        public int Size;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ToggleKeys
    {
        public int Size;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FilterKeys
    {
        public int Size;
        public int Flags;
        public int WaitMs;
        public int DelayMs;
        public int RepeatMs;
        public int BounceMs;
    }
}

public sealed record KeyboardSnapshot(
    int Speed,
    int Delay,
    int StickyFlags,
    int ToggleFlags,
    int FilterFlags,
    int FilterWaitMs,
    int FilterDelayMs,
    int FilterRepeatMs,
    int FilterBounceMs);
