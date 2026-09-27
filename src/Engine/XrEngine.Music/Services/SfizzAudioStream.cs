using Sfizz;
using XrEngine.Media;

namespace XrEngine.Music
{
    public class SfizzAudioStream : IAudioStream
    {
        private int _lastSampleOffset;
        private readonly SfizzLib.Synth _synth;
        private readonly SfizzLib.Buffer _buffer;
        private readonly int _bufSizeSamples;
        private bool _isStreaming;

        public SfizzAudioStream(SfizzLib.Synth synth, SfizzLib.Buffer buffer, int bufSizeSamples, int sampleRate, int channels = 2)
        {
            _synth = synth;
            _buffer = buffer;
            _bufSizeSamples = bufSizeSamples;
            _lastSampleOffset = -1;

            Format = new AudioFormat
            {
                Channels = channels,
                SampleRate = sampleRate,
                SampleType = AudioSampleType.Float
            };
        }

        public unsafe int Fill(Span<byte> data, float timeSec)
        {
            if (Dispatcher is QueueDispatcher queue)
                queue.ProcessQueue();

            var SAMPLE_SIZE = Format.BitsPerSample / 8;
            var samples = data.Length / SAMPLE_SIZE;
            var written = 0;

            fixed (byte* dst = data)
            {
                while (written < samples)
                {
                    if (_lastSampleOffset == -1 || _lastSampleOffset >= _bufSizeSamples)
                    {
                        if (Dispatcher is LockDispatcher lockDisp)
                        {
                            lock (lockDisp.Lock)
                                _synth.render(_buffer, Format.SampleType != AudioSampleType.Float);
                        }
                        else
                            _synth.render(_buffer, Format.SampleType != AudioSampleType.Float);

                        _lastSampleOffset = 0;
                    }

                    var count = Math.Min(_bufSizeSamples - _lastSampleOffset, samples - written);

                    void* src = Format.SampleType == AudioSampleType.Float ?
                        _buffer.getFloatPointer() + _lastSampleOffset :
                        _buffer.getPcmPointer() + _lastSampleOffset;

                    Buffer.MemoryCopy(src, dst + written * SAMPLE_SIZE, (samples - written) * SAMPLE_SIZE, count * SAMPLE_SIZE);

                    _lastSampleOffset += count;
                    written += count;
                }
            }

            return written;
        }

        public void Start()
        {
            _isStreaming = true;
        }

        public void Stop()
        {
            _isStreaming = false;
        }

        internal IDispatcher? Dispatcher;

        public int PrefBufferSizeSamples => _bufSizeSamples;

        public int PrefBufferCount => 2;

        public float Length => 0;

        public AudioFormat Format { get; }

        public bool IsStreaming => _isStreaming;

    }
}
