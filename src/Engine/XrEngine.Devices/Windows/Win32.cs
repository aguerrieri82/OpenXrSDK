using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Devices.Windows
{
    public static partial class Win32
    {
        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void MidiInProc(IntPtr hMidiIn, uint wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void MidiOutProc(IntPtr hMidiOut, uint wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        public const uint CALLBACK_FUNCTION = 0x00030000;
        
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiOutGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int midiOutGetDevCaps(uint uDeviceID, out MidiOutCaps lpMidiOutCaps, uint cbMidiOutCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int midiInGetDevCaps(uint uDeviceID, out MidiInCaps lpMidiInCaps, uint cbMidiInCaps);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiOutOpen(out IntPtr lphMidiOut, uint uDeviceID, MidiOutProc? dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiOutClose(IntPtr hMidiOut);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiInOpen(out IntPtr lphMidiIn, uint uDeviceID, MidiInProc? dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiInClose(IntPtr hMidiIn);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int midiOutGetErrorText(int mmrError, StringBuilder pszText, uint cchText);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int midiInGetErrorText(int mmrError, StringBuilder pszText, uint cchText);

        [DllImport("winmm.dll")]
        public static extern uint timeGetTime();


        // MIDIOUTCAPS (rough mapping based on Windows SDK)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MidiOutCaps
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }

        // MIDIINCAPS (rough mapping based on Windows SDK)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MidiInCaps
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwSupport;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MidiHdr
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);
            
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiInStart(IntPtr hMidiIn);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern int midiInStop(IntPtr hMidiIn);
    }
}
