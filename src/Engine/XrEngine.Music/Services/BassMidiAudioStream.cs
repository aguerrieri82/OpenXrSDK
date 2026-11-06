
using ManagedBass;
using XrEngine.Media;

namespace XrEngine.Music
{
    [AIGenerated]
    public class BassMidiAudioStream : IAudioStream
    {
        private int _stream;
        private readonly object _lock;
        private volatile bool _isStreaming;

        public BassMidiAudioStream(int stream, int sampleRate, object syncLock)
        {
            _stream = stream;
            _lock = syncLock;

            Format = new AudioFormat
            {
                Channels = 2,
                SampleRate = sampleRate,
                SampleType = AudioSampleType.Float
            };
        }

        public unsafe int Fill(Span<byte> data, float timeSec)
        {
            lock (_lock)
            {
                if (_stream == 0)
                    return 0;

                fixed (byte* ptr = data)
                {
                    int length = Bass.ChannelGetData(_stream, (IntPtr)ptr, data.Length);

                    if (length < 0)
                        throw new InvalidOperationException($"BASS rendering failed: {Bass.LastError}");

                    return length / sizeof(float);
                }
            }
        }

        public void Start()
        {
            _isStreaming = true;
        }

        public void Stop()
        {
            _isStreaming = false;
        }

        internal void Close()
        {
            lock (_lock)
            {
                _stream = 0;
                _isStreaming = false;
            }
        }

        public int PrefBufferSizeSamples => 256;

        public int PrefBufferCount => 2;

        public float Length => 0;

        public AudioFormat Format { get; }

        public bool IsStreaming => _isStreaming;
    }
}
