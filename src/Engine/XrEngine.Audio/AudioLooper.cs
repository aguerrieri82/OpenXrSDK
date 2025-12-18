using OpenAl.Framework;
using System.Diagnostics;
using XrEngine.Media;


namespace XrEngine.Audio
{
    public class AudioLooper : IAudioStream
    {
        protected bool _isStreaming;
        protected byte[]? _nextBuffer;
        protected bool _isFading;
        protected float _prevBufferSize;
        protected int _lastSample;

        public void LoadBuffer(byte[] buffer)
        {
            if (Loop!.Buffer == null || Loop.Buffer.Length == 0)
                Loop.Buffer = buffer;
            _nextBuffer = buffer;

        }

        protected virtual void LoadNextBuffer()
        {

        }

        public unsafe virtual int Fill(Span<byte> data, float timeSec)
        {
            Debug.Assert(Loop?.Format != null);

            float duration = Loop.Duration();

            int fadeStartSample = Loop.Format.TimeToSample(duration - FadeSize);
            int fadeSizeSamples = Loop.Format.TimeToSample(FadeSize);

            fixed (byte* pLoop = Loop.Buffer, pData = data, pNext = _nextBuffer)
            {
                short* sLoop = (short*)pLoop;
                short* sData = (short*)pData;
                short* sNext = (short*)pNext;

                int count = data.Length / 2;

                int l = _lastSample;


                for (int i = 0; i < count; i++)
                {
                    if (l >= fadeStartSample)
                    {
                        if (l == fadeStartSample)
                            LoadNextBuffer();

                        int fadeSample = (l - fadeStartSample);
                        float fadeFactor = fadeSample / (float)fadeSizeSamples;
                        short mixWith = sNext != null ? sNext[fadeSample] : sLoop[fadeSample];
                        sData[i] = (short)((sLoop[l] * (1 - fadeFactor)) + (mixWith * fadeFactor));
                    }
                    else
                        sData[i] = sLoop[l];

                    l = (l + 1) % (Loop.Buffer!.Length / 2);

                    if (l == 0)
                    {
                        l = fadeSizeSamples;
                        if (_nextBuffer != null)
                            Loop.Buffer = _nextBuffer;
                    }
                }

                _lastSample = l;
            }

            return data.Length;
        }

        public void Start()
        {
            _isStreaming = true;
        }

        public void Stop()
        {
            _isStreaming = false;
        }

        public AudioData? Loop { get; set; }

        public float FadeSize { get; set; }

        public int PrefBufferSizeSamples => (int)(0.1f * 44100);

        public int PrefBufferCount => 2;

        public float Length => 0;

        public AudioFormat Format => Loop?.Format == null ? throw new NullReferenceException() :
                                     AudioFormatConverter.ToAudioFormat(Loop.Format);

        public bool IsStreaming => _isStreaming;

    }
}
