using Fftw;
using System.Diagnostics;
using System.Numerics;

namespace XrEngine.Audio
{
    public class PitchShiftFilter : IAudioFilter
    {
        private int _hopSize;
        private int _numFrames;
        private double[]? _window;
        private double[]? _phaseAccum;
        private double[]? _prevPhase;
        private FftwBuffer<double>? _fftIn;
        private FftwBuffer<Complex>? _fftOut;
        private FftwBuffer<Complex>? _shiftedSpectrum;
        private int _sampleRate;
        private int _inputLen;

        private static double[] HannWindow(int size)
        {
            double[] window = new double[size];
            for (int i = 0; i < size; i++)
                window[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)));

            return window;
        }

        public void Initialize(int inputLen, int sampleRate)
        {
            if (inputLen == _inputLen && sampleRate == _sampleRate)
                return;

            var windowSize = FFtSize;

            _hopSize = windowSize / 4;
            _numFrames = (inputLen - windowSize) / _hopSize + 1;

            _window = HannWindow(windowSize);

            _phaseAccum = new double[windowSize];
            _prevPhase = new double[windowSize];

            _fftIn = new FftwBuffer<double>(windowSize);
            _fftOut = new FftwBuffer<Complex>(windowSize / 2 + 1);

            _shiftedSpectrum = new FftwBuffer<Complex>(_fftOut.Length);

            _sampleRate = sampleRate;

            _inputLen = inputLen;
        }


        public unsafe void Transform(Span<float> input, Span<float> output)
        {
            Debug.Assert(_fftIn != null && _fftOut != null && _prevPhase != null && _shiftedSpectrum != null && _phaseAccum != null);

            var windowSize = FFtSize;

            if (_numFrames > 1)
            {
                _phaseAccum.AsSpan().Fill(0);
                _prevPhase.AsSpan().Fill(0);
            }

            _shiftedSpectrum.ToSpan().Fill(0);

            for (int frame = 0; frame < _numFrames; frame++)
            {
                var frameOfs = frame * _hopSize;

                for (int i = 0; i < _fftIn.Length; i++)
                    _fftIn.Pointer[i] = input[frameOfs + i];

                FftwLib.Dft(_fftIn, _fftOut);

                for (int i = 0; i < _shiftedSpectrum.Length; i++)
                {
                    int shiftedIndex = (int)(i * Factor);
                    if (shiftedIndex < _shiftedSpectrum.Length && shiftedIndex >= 0)
                        _shiftedSpectrum.Pointer[shiftedIndex] = _fftOut.Pointer[i];
                }

                // Phase vocoder adjustment
                if (_numFrames > 1)
                {
                    var phaseFactor = (2.0 * Math.PI * _hopSize / _sampleRate);

                    for (int i = 0; i < _shiftedSpectrum.Length; i++)
                    {
                        var magnitude = _shiftedSpectrum.Pointer[i].Magnitude;
                        var phase = _shiftedSpectrum.Pointer[i].Phase;

                        var deltaPhase = phase - _prevPhase[i];
                        _prevPhase[i] = phase;

                        var trueFreq = deltaPhase / phaseFactor;
                        _phaseAccum[i] += trueFreq;

                        _shiftedSpectrum.Pointer[i] = Complex.FromPolarCoordinates(magnitude, _phaseAccum[i]);
                    }
                }

                // IFFT

                FftwLib.Dft(_shiftedSpectrum, _fftIn);

                // Overlap-add
                for (int i = 0; i < windowSize; i++)
                    output[frameOfs + i] += (float)(_fftIn.Pointer[i] / _fftIn.Length);
            }
        }

        public float Factor { get; set; }

        public int FFtSize { get; set; }
    }
}
