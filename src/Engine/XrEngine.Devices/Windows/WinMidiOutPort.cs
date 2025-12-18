using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Devices.Windows
{
    public class WinMidiOutPort : IMidiOutPort
    {
        IntPtr _hOut;
        readonly uint _deviceIndex;
        bool _opened;

        public WinMidiOutPort(uint deviceIndex)
        {
            _deviceIndex = deviceIndex;
            Open();
        }

        void Open()
        {
            if (_opened)
                return;

            var res = Win32.midiOutOpen(out _hOut, _deviceIndex, null, IntPtr.Zero, 0);
            if (res != 0)
                throw new InvalidOperationException($"midiOutOpen failed: {GetOutError(res)}");

            _opened = true;
        }

        public void Send(byte[] data, int offset, int count)
        {
            if (!_opened)
                throw new ObjectDisposedException(nameof(WinMidiOutPort));

            if (offset < 0 || count < 0 || offset + count > data.Length) 
                throw new ArgumentOutOfRangeException();

            uint msg = 0;
            for (int i = 0; i < Math.Min(3, count); ++i)
                msg |= (uint)data[offset + i] << (8 * i);

            var r = Win32.midiOutShortMsg(_hOut, msg);
            if (r != 0)
                throw new InvalidOperationException($"midiOutShortMsg failed: {GetOutError(r)}");
        }

        public void Close()
        {
            if (!_opened)
                return;

            var res = Win32.midiOutClose(_hOut);
            if (res != 0)
                throw new InvalidOperationException($"midiOutClose failed: {GetOutError(res)}");

            _hOut = IntPtr.Zero;
            _opened = false;
        }

        static string GetOutError(int code)
        {
            var sb = new StringBuilder(512);
            _ = Win32.midiOutGetErrorText(code, sb, (uint)sb.Capacity);
            return sb.ToString();
        }
    }
}
