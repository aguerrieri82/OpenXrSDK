using OpenAl.Framework;
using System.Diagnostics;


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

        public unsafe virtual int Fill(byte[] data, float timeSec)
        {
            Debug.Assert(Loop?.Format != null);

            var duration = Loop.Duration();

            var fadeStartSample = Loop.Format.TimeToSample(duration - FadeSize);
            var fadeSizeSamples = Loop.Format.TimeToSample(FadeSize);

            fixed (byte* pLoop = Loop.Buffer, pData = data, pNext = _nextBuffer )
            {
                short* sLoop = (short*)pLoop;
                short* sData = (short*)pData;
                short* sNext = (short*)pNext;

                var count = data.Length / 2;

                var l = _lastSample;

        
                for (var i = 0; i < count; i++)
                {
                    if (l >= fadeStartSample)
                    {
                        if (l == fadeStartSample)
                            LoadNextBuffer();

                        var fadeSample = (l - fadeStartSample);
                        var fadeFactor = fadeSample / (float)fadeSizeSamples;
                        var mixWith = sNext != null ? sNext[fadeSample] : sLoop[fadeSample];
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

        public AudioData? Loop {  get; set; }   

        public float FadeSize { get; set; }

        public int PrefBufferSize => (int)(0.1f * 44100 * 2);

        public int PrefBufferCount => 2;

        public float Length => 0;

        public AudioFormat Format => Loop?.Format ?? throw new NullReferenceException();

        public bool IsStreaming => _isStreaming;

    }
}
