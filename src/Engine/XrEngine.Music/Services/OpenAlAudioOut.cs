using OpenAl.Framework;
using Silk.NET.OpenAL;
using System.Diagnostics;
using XrEngine.Audio;
using XrEngine.Devices;
using XrEngine.Media;

namespace XrEngine.Music
{
    public class OpenAlAudioOut : IAudioOut
    {
        private readonly AlSource _source;
        private readonly Dictionary<byte[], AlBuffer> _buffers = [];
        private readonly AL _al;
        private AlAudioFormat? _format;
        private int _queueCount;

        public OpenAlAudioOut(AL al)
        {
            _source = new AlSource(al);
            _al = al;
        }

        public void Close()
        {
            _format = null;
            _source.Stop();
        }

        public byte[]? Dequeue(int timeoutMs)
        {
            if (_queueCount == 0)
                return null;

            var startTime = ReferenceClock.Default.Now / 1000000;

            while (_source.BuffersProcessed == 0)
            {
                EngineNativeLib.SleepFor(1 * 1000000);
                var curTime = ReferenceClock.Default.Now / 1000000;
                if ((curTime - startTime) > (ulong)timeoutMs && timeoutMs > 0)
                    return null;
            }

            var alBuffer = _source.DequeueBuffers(1).First();

            _queueCount--;

            return _buffers.First(a => a.Value == alBuffer).Key;
        }

        public void Enqueue(byte[] buffer)
        {
            Debug.Assert(_format != null);

            if (!_buffers.TryGetValue(buffer, out var alBuffer))
            {
                alBuffer = new AlBuffer(_al);
                _buffers[buffer] = alBuffer;
            }

            alBuffer.SetData(buffer, _format);

            _source.QueueBuffer(alBuffer);

            if (_source.State != SourceState.Playing)
                _source.Play();

            _queueCount++;

        }

        public void Open(AudioFormat format)
        {
            _format = AudioFormatConverter.ToAlAudioFormat(format);
        }

        public void Reset()
        {
            _source.Stop();

            while (_queueCount > 0)
                Dequeue(1);
        }

        public float Volume
        {
            get => _source.Gain;
            set
            {
                if (value > _source.MaxGain)
                    _source.MaxGain = value;
                if (value < _source.MinGain)
                    _source.MinGain = value;
                _source.Gain = value;
            }
        }


        public AlSource Source => _source;
    }
}
