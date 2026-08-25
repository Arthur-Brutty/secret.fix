using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SecretFix.Core;

namespace SecretFix.Services;

public sealed class InputBenchmarkService : IDisposable
{
    private const int WmInput = 0x00FF;
    private const uint RidInput = 0x10000003;
    private const uint RimTypeMouse = 0;
    private HwndSource? _source;
    private readonly List<double> _intervals = [];
    private long _lastTimestamp;
    private long _startedTimestamp;
    private int _eventCount;
    private bool _running;

    public bool IsRunning => _running;

    public void Start(WindowInteropHelper window)
    {
        if (_running) return;
        var handle = window.Handle;
        if (handle == IntPtr.Zero) throw new InvalidOperationException("A janela ainda não possui handle para Raw Input.");
        _source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("Não foi possível registrar Raw Input na janela.");
        var devices = new[] { new RawInputDevice { UsagePage = 0x01, Usage = 0x02, Flags = 0, Target = handle } };
        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>()))
            throw new InvalidOperationException($"Registro Raw Input falhou ({Marshal.GetLastWin32Error()}).");
        _intervals.Clear();
        _eventCount = 0;
        _lastTimestamp = 0;
        _startedTimestamp = Stopwatch.GetTimestamp();
        _source.AddHook(WndProc);
        _running = true;
    }

    public InputBenchmarkResult Stop()
    {
        if (!_running) return InputBenchmarkResult.Empty(TimeSpan.Zero);
        _running = false;
        _source?.RemoveHook(WndProc);
        _source = null;
        var duration = Stopwatch.GetElapsedTime(_startedTimestamp);
        return InputBenchmarkCalculator.Calculate(_intervals, duration, _eventCount);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!_running || message != WmInput) return IntPtr.Zero;
        uint size = 0;
        if (GetRawInputData(lParam, RidInput, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != 0 || size == 0)
            return IntPtr.Zero;
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(lParam, RidInput, buffer, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != size)
                return IntPtr.Zero;
            var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RimTypeMouse) return IntPtr.Zero;
            var now = Stopwatch.GetTimestamp();
            if (_lastTimestamp != 0)
                _intervals.Add((now - _lastTimestamp) * 1000d / Stopwatch.Frequency);
            _lastTimestamp = now;
            _eventCount++;
        }
        finally { Marshal.FreeHGlobal(buffer); }
        return IntPtr.Zero;
    }

    public void Dispose() => Stop();

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice { public ushort UsagePage; public ushort Usage; public uint Flags; public IntPtr Target; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader { public uint Type; public uint Size; public IntPtr Device; public IntPtr WParam; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint number, uint size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);
}
