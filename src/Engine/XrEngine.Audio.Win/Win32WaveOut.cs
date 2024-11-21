using Common.Interop;
using OpenAl.Framework;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Silk.NET.Core.Native.WinString;

namespace XrEngine.Audio
{
    public unsafe class Win32WaveOut : IAudioOut, IDisposable
    {
        #region WIN32

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutOpen(out IntPtr hWaveOut, uint uDeviceID, ref WaveFormat lpFormat, IntPtr dwCallback, int dwInstance, uint fdwOpen);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, WaveHeader* lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutWrite(IntPtr hWaveOut, WaveHeader* lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, WaveHeader* lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutGetErrorText(int mmrError, StringBuilder pszText, uint cchText);


        private delegate void WaveOutProcDelegate(IntPtr hWaveOut, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [StructLayout(LayoutKind.Sequential)]
        struct WaveFormat
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WaveHeader
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public WaveHeaderFlags dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [Flags]
        enum WaveHeaderFlags : int
        {
            None = 0,
            Done = 1,
            Prepared  = 2,
            BeginLoop = 4,
            EndLoop = 8,
            InQueue = 0x10
        }

        const ushort WAVE_FORMAT_PCM = 1;
        const uint WAVE_MAPPER = 0xFFFFFFFF;
        const uint CALLBACK_FUNCTION = 0x00030000;
        const uint WOM_DONE = 0x3BD;

        #endregion

        #region AudioBuffer

        class AudioBuffer : IDisposable
        {
            public AudioBuffer()
            {
                Event = new ManualResetEvent(false);
                Header.Value = new WaveHeader();
            }

            public void Dispose()
            {
                Header.Dispose();
                Event.Dispose();
                Buffer?.Dispose();
                GC.SuppressFinalize(this);
            }

            public uint Id;

            public NativeStruct<WaveHeader> Header;

            public ArrayMemoryBuffer<byte>? Buffer;

            public ManualResetEvent Event;
        }

        #endregion

        readonly List<AudioBuffer> _buffers = [];
        nint _hWaveOut;
        uint _lastBufId;
        GCHandle _procGCHandle;

        public unsafe void Enqueue(byte[] buffer)
        {
            var aBuffer = _buffers.FirstOrDefault(a => a.Buffer?.Data == buffer);

            if (aBuffer == null)
            {
                aBuffer = new AudioBuffer()
                {
                    Buffer = new ArrayMemoryBuffer<byte>(buffer),
                    Id = _lastBufId++
                };

                aBuffer.Header.ValueRef.dwBufferLength = (uint)buffer.Length;
                aBuffer.Header.ValueRef.lpData = (nint)aBuffer.Buffer.Lock();
                aBuffer.Header.ValueRef.dwUser = (nint)aBuffer.Id;

                CheckError(waveOutPrepareHeader(_hWaveOut, aBuffer.Header, (uint)Marshal.SizeOf<WaveHeader>()));

                lock (_buffers)
                    _buffers.Add(aBuffer);
            }


            aBuffer.Header.ValueRef.dwFlags &= ~(WaveHeaderFlags.Done);

            aBuffer.Event.Reset();

            CheckError(waveOutWrite(_hWaveOut, aBuffer.Header, (uint)Marshal.SizeOf<WaveHeader>()));
        }

        protected void CheckError(int res)
        {
            if (res == 0)
                return;

            var buffer = new StringBuilder(512);
            _ = waveOutGetErrorText(res, buffer, (uint)buffer.Capacity);

            Log.Warn(this, buffer.ToString());
        }

        public byte[]? Dequeue(int timeoutMs)
        {
            AudioBuffer? aBuffer = null;

            lock (_buffers)
            {
                if (_buffers.Count == 0)
                    return null;

                if (timeoutMs == 0)
                    aBuffer = _buffers.FirstOrDefault(a => (a.Header.ValueRef.dwFlags & WaveHeaderFlags.Done) != 0);
            }

            if (aBuffer == null && timeoutMs > 0)
            {
                AudioBuffer[] buffers;

                lock (_buffers)
                    buffers = [.. _buffers];

                var events = buffers.Select(a => a.Event).ToArray();
                
                var index = WaitHandle.WaitAny(events, timeoutMs);
                
                if (index == WaitHandle.WaitTimeout)
                    return null;

                aBuffer = buffers[index];
            }

            return aBuffer?.Buffer?.Data;
        }

        public void Reset()
        {
            CheckError(waveOutReset(_hWaveOut));

            lock (_buffers)
            {
                foreach (var buffer in _buffers)
                {
                    Debug.Assert((buffer.Header.ValueRef.dwFlags & WaveHeaderFlags.InQueue) == 0);
                    CheckError(waveOutUnprepareHeader(_hWaveOut, buffer.Header, (uint)Marshal.SizeOf<WaveHeader>()));
                    buffer.Dispose();
                }
                _buffers.Clear();
            }
        }

        public void Open(AudioFormat format)
        {
            var wFormat = new WaveFormat
            {
                cbSize = (ushort) Marshal.SizeOf<WaveFormat>(),
                nChannels = (ushort)format.Channels,
                wBitsPerSample = (ushort)format.BitsPerSample,
                nSamplesPerSec= (ushort)format.SampleRate,
                nBlockAlign= (ushort)(format.BitsPerSample * format.Channels / 8),
                nAvgBytesPerSec = (uint)((format.BitsPerSample * format.Channels / 8) * format.SampleRate),
                wFormatTag= WAVE_FORMAT_PCM
            };

            var del = (WaveOutProcDelegate)WaveOutProc;
            _procGCHandle = GCHandle.Alloc(del);

            CheckError(waveOutOpen(out _hWaveOut, WAVE_MAPPER, ref wFormat, Marshal.GetFunctionPointerForDelegate(del), 0, CALLBACK_FUNCTION));
        }

        unsafe void WaveOutProc(IntPtr hWaveOut, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (uMsg == WOM_DONE)
            {
                var header = (WaveHeader*)dwParam1;
                var bufId = header->dwUser;

                //Log.Debug(this, $"DONE {bufId} {header->dwFlags}");

                AudioBuffer? aBuffer;

                lock (_buffers)
                    aBuffer = _buffers.FirstOrDefault(a => a.Id == bufId);

                if (aBuffer == null)
                    Debugger.Break();

                aBuffer?.Event.Set();
            }
        }

        public void Close()
        {
            Reset();

            CheckError(waveOutClose(_hWaveOut));

            _hWaveOut = 0;
            _procGCHandle.Free();
        }

        public void Dispose()
        {
            if (_hWaveOut != 0)
                Close();
            GC.SuppressFinalize(this);
        }
    }
}
