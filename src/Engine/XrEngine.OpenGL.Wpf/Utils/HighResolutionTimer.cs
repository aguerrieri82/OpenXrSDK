using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XrEngine.Wpf;

public sealed class HighResolutionTimer : IDisposable
{
    #region INTEROP

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint CreateWaitableTimerExW(
        nint timerAttributes,
        string? timerName,
        uint flags,
        uint desiredAccess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetWaitableTimer(
        nint timer,
        ref long dueTime,
        int period,
        nint completionRoutine,
        nint argument,
        bool resume);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(nint handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint handle);

    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerAllAccess = 0x001F0003;
    private const uint Infinite = 0xFFFFFFFF;
    private const uint WaitObject0 = 0;

    #endregion

    private nint _handle;

    public HighResolutionTimer()
    {
        _handle = CreateWaitableTimerExW(
            0,
            null,
            CreateWaitableTimerHighResolution,
            TimerAllAccess);

        if (_handle == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Sleep(float seconds)
    {
        Debug.Assert(seconds >= 0);

        if (seconds <= 0)
            return;

        Sleep(TimeSpan.FromSeconds(seconds));
    }

    public void Sleep(TimeSpan duration)
    {
        Debug.Assert(duration >= TimeSpan.Zero);

        if (duration <= TimeSpan.Zero)
            return;

        long dueTime = -duration.Ticks;

        if (!SetWaitableTimer(_handle, ref dueTime, 0, 0, 0, false))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        uint result = WaitForSingleObject(_handle, Infinite);

        if (result != WaitObject0)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public void Dispose()
    {
        if (_handle == 0)
            return;

        CloseHandle(_handle);
        _handle = 0;

        GC.SuppressFinalize(this);
    }

}