using NAudio.Wave;


namespace Sfizz
{
    public class SfizzSampleProvider : ISampleProvider
    {
        private readonly WaveFormat _format;
        private int _lastOffset;
        private readonly SfizzLib.Synth _synth;
        private readonly SfizzLib.Buffer _buffer;
        private readonly int _bufSize;


        public SfizzSampleProvider(SfizzLib.Synth synth, SfizzLib.Buffer buffer, int bufSize, int sampleRate, int channels = 2)
        {
            _synth = synth;
            _buffer = buffer;
            _bufSize = bufSize;
            _lastOffset = -1;
            _format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }


        public unsafe int Read(float[] buffer, int offset, int count)
        {
            if (_lastOffset == -1 || _lastOffset >= _bufSize - 1)
            {
                _synth.render(_buffer, false);
                _lastOffset = 0;
            }

            var src = _buffer.getFloatPointer() + _lastOffset;

            fixed (float* dst = &buffer[offset])
            {
                var max = Math.Min(_bufSize - _lastOffset, count);
                Buffer.MemoryCopy(src, dst, (buffer.Length - offset) * sizeof(float), max * sizeof(float));
                _lastOffset += max;
                return max;
            }
        }


        public WaveFormat WaveFormat => _format;
    }
}
