using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Devices.Windows
{
    class WinMidiInPort : IMidiInPort
    {
        const uint MIM_OPEN = 0x3C1;
        const uint MIM_CLOSE = 0x3C2;
        const uint MIM_DATA = 0x3C3;
        const uint MIM_LONGDATA = 0x3C4;
        const uint MIM_ERROR = 0x3C5;
        const uint MIM_LONGERROR = 0x3C6;

        IntPtr _hIn = IntPtr.Zero;
        readonly uint _deviceIndex;
        bool _opened;
        Win32.MidiInProc? _proc;
        GCHandle _gch;

        public WinMidiInPort(uint deviceIndex)
        {
            _deviceIndex = deviceIndex;
            Open();
        }

        void Open()
        {
            if (_opened)
                return;


            _proc = new Win32.MidiInProc(MidiInCallback);

            _gch = GCHandle.Alloc(_proc, GCHandleType.Normal);

            int res = Win32.midiInOpen(out _hIn, _deviceIndex, _proc, IntPtr.Zero, Win32.CALLBACK_FUNCTION);
            if (res != 0)
                throw new InvalidOperationException($"midiInOpen failed: {GetInError(res)}");

            int r2 = Win32.midiInStart(_hIn);
            if (r2 != 0)
            {
                _gch.Free();
                Win32.midiInClose(_hIn);
                _hIn = IntPtr.Zero;
                throw new InvalidOperationException($"midiInStart failed: {GetInError(r2)}");
            }

            _opened = true;
        }

        static int GetShortMessageLength(byte status)
        {
            // System realtime messages are single byte (>= 0xF8)
            if (status >= 0xF8)
                return 1;

            // System common messages (0xF0..0xF7) vary, treat as 1 for simplicity except SysEx (0xF0) which is long
            if (status >= 0xF0)
                return 1;

            byte m = (byte)(status & 0xF0);

            // Program Change (0xC0) and Channel Pressure (0xD0) are 2 bytes
            if (m == 0xC0 || m == 0xD0)
                return 2;

            // Note on/off, control change, pitch wheel, aftertouch etc. are 3 bytes
            return 3;
        }

        void MidiInCallback(IntPtr hMidiIn, uint wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            try
            {
                switch (wMsg)
                {
                    case MIM_DATA:
                        {
                            // dwParam1 contains packed short message (DWORD)
                            uint packed = (uint)dwParam1.ToInt64();
                            byte b0 = (byte)(packed & 0xFF);
                            byte b1 = (byte)((packed >> 8) & 0xFF);
                            byte b2 = (byte)((packed >> 16) & 0xFF);

                            int count = GetShortMessageLength(b0);
                            byte[] bytes = [b0, b1, b2];
                            byte[] data = count switch
                            {
                                1 => [b0],
                                2 => [b0, b1],
                                _ => bytes,
                            };

                            long timestamp = dwParam2.ToInt64();

                            OnDataReceived(new MidiData
                            {
                                Data = data,
                                Offset = 0,
                                Count = data.Length,
                                Timestamp = timestamp
                            });
                        }
                        break;

                    case MIM_LONGDATA:
                        {
                            // dwParam1 -> pointer to MIDIHDR
                            Win32.MidiHdr hdr = Marshal.PtrToStructure<Win32.MidiHdr>(dwParam1);
                            if (hdr.dwBytesRecorded > 0 && hdr.lpData != IntPtr.Zero)
                            {
                                int count = (int)hdr.dwBytesRecorded;
                                byte[] data = new byte[count];
                                Marshal.Copy(hdr.lpData, data, 0, count);

                                long timestamp = dwParam2.ToInt64();

                                OnDataReceived(new MidiData
                                {
                                    Data = data,
                                    Offset = 0,
                                    Count = count,
                                    Timestamp = timestamp
                                });
                            }
                        }
                        break;

                    case MIM_OPEN:
                    case MIM_CLOSE:
                    case MIM_ERROR:
                    case MIM_LONGERROR:
                    default:
                        // no-op for now
                        break;
                }
            }
            catch
            {
                // swallow to avoid crashing native callback thread.
            }
        }

        protected virtual void OnDataReceived(MidiData data)
        {
            DataReceived?.Invoke(this, data);
        }

        public void Close()
        {
            if (!_opened)
                return;

            _ = Win32.midiInStop(_hIn);
            int res = Win32.midiInClose(_hIn);
            if (res != 0)
                throw new InvalidOperationException($"midiInClose failed: {GetInError(res)}");

            _gch.Free();

            _hIn = IntPtr.Zero;
            _proc = null;
            _opened = false;
        }

        public ulong RefTimeMs => Win32.timeGetTime();

        static string GetInError(int code)
        {
            StringBuilder sb = new StringBuilder(512);
            _ = Win32.midiInGetErrorText(code, sb, (uint)sb.Capacity);
            return sb.ToString();
        }

        public event EventHandler<MidiData>? DataReceived;
    }
}
