using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace XrEngine.Wpf;

public static class DisplayUtils
{
    #region INTEROP


    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint flags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfoEx monitorInfo);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsW(string deviceName, int modeNum, ref DevMode devMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public DevModeUnion Union;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;

        public ushort LogPixels;
        public uint BitsPerPel;
        public uint PelsWidth;
        public uint PelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint IcmMethod;
        public uint IcmIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct DevModeUnion
    {
        [FieldOffset(0)]
        public PrinterFields Printer;

        [FieldOffset(0)]
        public Point Position;

        [FieldOffset(8)]
        public uint DisplayOrientation;

        [FieldOffset(12)]
        public uint DisplayFixedOutput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterFields
    {
        public short Orientation;
        public short PaperSize;
        public short PaperLength;
        public short PaperWidth;
        public short Scale;
        public short Copies;
        public short DefaultSource;
        public short PrintQuality;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    #endregion
    
    private const int EnumCurrentSettings = -1;
    private const uint MonitorDefaultToNearest = 0x00000002;

    public static uint GetRefreshRate(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;

        if (hwnd == 0)
            throw new InvalidOperationException("The WPF window has no native handle.");

        return GetRefreshRate(hwnd);
    }

    public static uint GetRefreshRate(nint hwnd)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);

        if (monitor == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var monitorInfo = new MonitorInfoEx
        {
            Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
            DeviceName = string.Empty
        };

        if (!GetMonitorInfoW(monitor, ref monitorInfo))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var mode = new DevMode
        {
            Size = (ushort)Marshal.SizeOf<DevMode>(),
            DeviceName = string.Empty,
            FormName = string.Empty
        };

        if (!EnumDisplaySettingsW(monitorInfo.DeviceName, EnumCurrentSettings, ref mode))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return mode.DisplayFrequency;
    }
}