using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SecretFix.Infrastructure.Windows;

public sealed class WindowsInputService
{
    private const uint SPI_GETMOUSE = 0x0003;
    private const uint SPI_SETMOUSE = 0x0004;
    private const uint SPI_GETMOUSESPEED = 0x0070;
    private const uint SPI_SETMOUSESPEED = 0x0071;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;
    private const uint Persist = SPIF_UPDATEINIFILE | SPIF_SENDCHANGE;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);

    public MouseSnapshot ReadMouse()
    {
        var ptr = Marshal.AllocHGlobal(sizeof(int) * 3);
        var speedPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            if (!SystemParametersInfo(SPI_GETMOUSE, 0, ptr, 0)) ThrowLastError();
            if (!SystemParametersInfo(SPI_GETMOUSESPEED, 0, speedPtr, 0)) ThrowLastError();
            var values = new int[3];
            Marshal.Copy(ptr, values, 0, 3);
            return new MouseSnapshot(values[0], values[1], values[2], Marshal.ReadInt32(speedPtr));
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
            Marshal.FreeHGlobal(speedPtr);
        }
    }

    public void ApplyLinearMouse(int speed = 10)
    {
        var values = new[] { 0, 0, 0 };
        var ptr = Marshal.AllocHGlobal(sizeof(int) * 3);
        try
        {
            Marshal.Copy(values, 0, ptr, values.Length);
            if (!SystemParametersInfo(SPI_SETMOUSE, 0, ptr, Persist)) ThrowLastError();
            if (!SystemParametersInfo(SPI_SETMOUSESPEED, 0, new IntPtr(speed), Persist)) ThrowLastError();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public void Restore(MouseSnapshot snapshot)
    {
        var values = new[] { snapshot.Threshold1, snapshot.Threshold2, snapshot.Acceleration };
        var ptr = Marshal.AllocHGlobal(sizeof(int) * 3);
        try
        {
            Marshal.Copy(values, 0, ptr, values.Length);
            if (!SystemParametersInfo(SPI_SETMOUSE, 0, ptr, Persist)) ThrowLastError();
            if (!SystemParametersInfo(SPI_SETMOUSESPEED, 0, new IntPtr(snapshot.Speed), Persist)) ThrowLastError();
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static void ThrowLastError() => throw new Win32Exception(Marshal.GetLastWin32Error());
}

public sealed record MouseSnapshot(int Threshold1, int Threshold2, int Acceleration, int Speed);
