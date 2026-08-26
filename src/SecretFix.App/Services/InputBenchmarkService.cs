using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SecretFix.Core;

namespace SecretFix.Services;

/// <summary>Raw Input v2 capture. Samples remain separated by hDevice.</summary>
public sealed class InputBenchmarkService : IDisposable
{
    private const int WmInput = 0x00FF;
    private const int WmInputDeviceChange = 0x00FE;
    private const uint RidInput = 0x10000003;
    private const uint RimTypeMouse = 0;
    private const uint RidevDevNotify = 0x2000;
    private const uint RidiDeviceName = 0x20000007;
    private HwndSource? _source;
    private readonly Dictionary<IntPtr, DeviceCapture> _captures = [];
    private long _startedTimestamp;
    private bool _running;

    public string? SelectedDevicePath { get; set; }
    public bool IsRunning => _running;
    public event EventHandler<string>? DeviceChanged;

    public void Start(WindowInteropHelper window)
    {
        if (_running) return;
        var handle = window.Handle;
        if (handle == IntPtr.Zero) throw new InvalidOperationException("The main window does not yet have a Raw Input handle.");
        _source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("Raw Input could not be registered on the main window.");
        var devices = new[] { new RawInputDevice { UsagePage = 0x01, Usage = 0x02, Flags = RidevDevNotify, Target = handle } };
        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>()))
            throw new InvalidOperationException($"Raw Input registration failed ({Marshal.GetLastWin32Error()}).");
        _captures.Clear();
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
        var selected = !string.IsNullOrWhiteSpace(SelectedDevicePath)
            ? _captures.Values.FirstOrDefault(capture => string.Equals(capture.DevicePath, SelectedDevicePath, StringComparison.OrdinalIgnoreCase))
            : _captures.Values.OrderByDescending(capture => capture.EventCount).FirstOrDefault();
        if (selected is null) return InputBenchmarkResult.Empty(duration);
        return InputConsistencyAnalyzer.Analyze(selected.Intervals, duration, selected.EventCount, selected.DevicePath) with
        {
            DeviceDisplayName = string.IsNullOrWhiteSpace(selected.DevicePath) ? "Generic HID Device" : selected.DevicePath
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!_running) return IntPtr.Zero;
        if (message == WmInputDeviceChange)
        {
            DeviceChanged?.Invoke(this, wParam == IntPtr.Zero ? "Raw Input device disconnected." : "Raw Input device changed.");
            return IntPtr.Zero;
        }
        if (message != WmInput) return IntPtr.Zero;
        uint size = 0;
        if (GetRawInputData(lParam, RidInput, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != 0 || size == 0) return IntPtr.Zero;
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(lParam, RidInput, buffer, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != size) return IntPtr.Zero;
            var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RimTypeMouse || header.Device == IntPtr.Zero) return IntPtr.Zero;
            if (!_captures.TryGetValue(header.Device, out var capture))
            {
                capture = new DeviceCapture(ReadDevicePath(header.Device));
                _captures.Add(header.Device, capture);
            }
            var now = Stopwatch.GetTimestamp();
            if (capture.LastTimestamp != 0) capture.Intervals.Add((now - capture.LastTimestamp) * 1000d / Stopwatch.Frequency);
            capture.LastTimestamp = now;
            capture.EventCount++;
        }
        finally { Marshal.FreeHGlobal(buffer); }
        return IntPtr.Zero;
    }

    private static string? ReadDevicePath(IntPtr device)
    {
        uint characters = 0;
        if (GetRawInputDeviceInfo(device, RidiDeviceName, IntPtr.Zero, ref characters) == uint.MaxValue || characters == 0) return null;
        var memory = Marshal.AllocHGlobal(checked((int)characters * sizeof(char)));
        try { return GetRawInputDeviceInfo(device, RidiDeviceName, memory, ref characters) == uint.MaxValue ? null : Marshal.PtrToStringUni(memory); }
        finally { Marshal.FreeHGlobal(memory); }
    }

    public void Dispose() => Stop();
    private sealed class DeviceCapture(string? devicePath) { public string? DevicePath { get; } = devicePath; public List<double> Intervals { get; } = []; public long LastTimestamp { get; set; } public int EventCount { get; set; } }
    [StructLayout(LayoutKind.Sequential)] private struct RawInputDevice { public ushort UsagePage; public ushort Usage; public uint Flags; public IntPtr Target; }
    [StructLayout(LayoutKind.Sequential)] private struct RawInputHeader { public uint Type; public uint Size; public IntPtr Device; public IntPtr WParam; }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint number, uint size);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint size);
}
