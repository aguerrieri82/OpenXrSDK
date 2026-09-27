using Fftw;
using System.Numerics;
using XrEngine.Media;

namespace XrEngine.Music
{
    public class PhaseVocoderTimeStretcher : IAudioTimeStretcher
    {
        readonly List<float> _input = [];
        readonly List<short> _output = [];

        AudioFormat? _format;

        FftwBuffer<double>? _fftIn;
        FftwBuffer<Complex>? _fftOut;
        FftwPlan _forwardPlan;
        FftwPlan _inversePlan;

        double[]? _window;
        double[][]? _prevPhase;
        double[][]? _phaseAccum;
        double[][]? _ola;
        double[]? _olaWeight;

        int _outputOffset;
        int _inputFrame;
        int _analysisHop;
        int _fftSize;

        double _hopAccumulator;
        float _tempo;

        bool _hasPhase;
        bool _configured;
        bool _flushed;
        bool _tailFlushed;

        public PhaseVocoderTimeStretcher()
        {
            _fftSize = 2048;
            _tempo = 1;
        }

        public void Configure(AudioFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);

            if (format.Channels <= 0 || format.SampleRate <= 0)
                throw new ArgumentException("Invalid audio format.", nameof(format));

            DisposeFft();

            _format = format;
            _analysisHop = _fftSize / 4;

            _window = new double[_fftSize];

            for (var i = 0; i < _fftSize; i++)
                _window[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / _fftSize);

            var bins = _fftSize / 2 + 1;

            _prevPhase = new double[format.Channels][];
            _phaseAccum = new double[format.Channels][];
            _ola = new double[format.Channels][];

            for (var ch = 0; ch < format.Channels; ch++)
            {
                _prevPhase[ch] = new double[bins];
                _phaseAccum[ch] = new double[bins];
                _ola[ch] = new double[_fftSize * 2];
            }

            _olaWeight = new double[_fftSize * 2];

            _fftIn = new FftwBuffer<double>(_fftSize);
            _fftOut = new FftwBuffer<Complex>(bins);

            _forwardPlan = FftwLib.DftPlan(_fftIn, _fftOut);
            _inversePlan = FftwLib.DftPlan(_fftOut, _fftIn);

            _configured = true;

            Reset();
        }

        public void SetTempo(float value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _tempo = value;
        }

        public void Reset()
        {
            _input.Clear();
            _output.Clear();

            _outputOffset = 0;
            _inputFrame = 0;
            _hopAccumulator = 0;

            _hasPhase = false;
            _flushed = false;
            _tailFlushed = false;

            if (_prevPhase != null)
            {
                foreach (var item in _prevPhase)
                    item.AsSpan().Clear();
            }

            if (_phaseAccum != null)
            {
                foreach (var item in _phaseAccum)
                    item.AsSpan().Clear();
            }

            if (_ola != null)
            {
                foreach (var item in _ola)
                    item.AsSpan().Clear();
            }

            _olaWeight?.AsSpan().Clear();
        }

        public void Write(ReadOnlySpan<short> input)
        {
            EnsureConfigured();

            if (_flushed)
                throw new InvalidOperationException("The stretcher has already been flushed.");

            if (Math.Abs(_tempo - 1f) < 0.000001f)
            {
                for (var i = 0; i < input.Length; i++)
                    _output.Add(input[i]);

                return;
            }

            for (var i = 0; i < input.Length; i++)
                _input.Add(input[i] / 32768f);

            Process();
        }

        public int Read(Span<short> output)
        {
            Process();

            var count = Math.Min(output.Length, _output.Count - _outputOffset);

            for (var i = 0; i < count; i++)
                output[i] = _output[_outputOffset + i];

            _outputOffset += count;

            if (_outputOffset > 32768 && _outputOffset * 2 > _output.Count)
            {
                _output.RemoveRange(0, _outputOffset);
                _outputOffset = 0;
            }

            return count;
        }

        public void Flush()
        {
            if (_flushed)
                return;

            _flushed = true;

            Process();

            FlushTail();
        }

        protected void Process()
        {
            if (!_configured || Math.Abs(_tempo - 1f) < 0.000001f)
                return;

            var channels = _format!.Channels;
            var inputFrames = _input.Count / channels;

            while (true)
            {
                var available = inputFrames - _inputFrame;

                if (available < _fftSize && !_flushed)
                    return;

                if (available <= 0)
                    break;

                ProcessFrame(Math.Min(available, _fftSize));

                _inputFrame += _analysisHop;

                if (_inputFrame > 16384)
                {
                    var samples = _inputFrame * channels;

                    _input.RemoveRange(0, samples);

                    inputFrames -= _inputFrame;
                    _inputFrame = 0;
                }
            }

            if (_flushed)
                FlushTail();
        }

        protected unsafe void ProcessFrame(int validFrames)
        {
            var channels = _format!.Channels;
            var bins = _fftSize / 2 + 1;

            _hopAccumulator += _analysisHop / (double)_tempo;

            var synthesisHop = Math.Max(1, (int)Math.Floor(_hopAccumulator));
            _hopAccumulator -= synthesisHop;

            for (var ch = 0; ch < channels; ch++)
            {
                for (var i = 0; i < _fftSize; i++)
                {
                    var sample = i < validFrames
                        ? _input[(_inputFrame + i) * channels + ch]
                        : 0;

                    _fftIn!.Pointer[i] = sample * _window![i];
                }

                _forwardPlan.Execute();

                for (var bin = 0; bin < bins; bin++)
                {
                    var value = _fftOut!.Pointer[bin];
                    var magnitude = value.Magnitude;
                    var phase = value.Phase;

                    if (!_hasPhase)
                    {
                        _prevPhase![ch][bin] = phase;
                        _phaseAccum![ch][bin] = phase;

                        continue;
                    }

                    var expected =
                        2.0 * Math.PI *
                        bin *
                        _analysisHop /
                        _fftSize;

                    var delta =
                        phase -
                        _prevPhase![ch][bin] -
                        expected;

                    delta = WrapPhase(delta);

                    var trueFrequency =
                        2.0 * Math.PI * bin / _fftSize +
                        delta / _analysisHop;

                    _phaseAccum![ch][bin] +=
                        trueFrequency * synthesisHop;

                    _prevPhase[ch][bin] = phase;

                    _fftOut.Pointer[bin] =
                        Complex.FromPolarCoordinates(
                            magnitude,
                            _phaseAccum[ch][bin]);
                }

                _inversePlan.Execute();

                for (var i = 0; i < _fftSize; i++)
                {
                    var value =
                        _fftIn!.Pointer[i] /
                        _fftSize *
                        _window![i];

                    _ola![ch][i] += value;
                }
            }

            _hasPhase = true;

            for (var i = 0; i < _fftSize; i++)
                _olaWeight![i] += _window![i] * _window[i];

            EmitFrames(synthesisHop);
        }

        protected void EmitFrames(int frames)
        {
            var channels = _format!.Channels;

            frames = Math.Min(frames, _olaWeight!.Length);

            for (var frame = 0; frame < frames; frame++)
            {
                var weight = _olaWeight[frame];

                for (var ch = 0; ch < channels; ch++)
                {
                    var value = weight > 1e-12
                        ? _ola![ch][frame] / weight
                        : 0;

                    AddOutput(value);
                }
            }

            ShiftOla(frames);
        }

        protected void ShiftOla(int frames)
        {
            if (frames <= 0)
                return;

            foreach (var channel in _ola!)
            {
                Array.Copy(
                    channel,
                    frames,
                    channel,
                    0,
                    channel.Length - frames);

                Array.Clear(
                    channel,
                    channel.Length - frames,
                    frames);
            }

            Array.Copy(
                _olaWeight!,
                frames,
                _olaWeight!,
                0,
                _olaWeight.Length - frames);

            Array.Clear(
                _olaWeight,
                _olaWeight.Length - frames,
                frames);
        }

        protected void FlushTail()
        {
            if (_tailFlushed ||
                !_configured ||
                Math.Abs(_tempo - 1f) < 0.000001f)
                return;

            var last = -1;

            for (var i = _olaWeight!.Length - 1; i >= 0; i--)
            {
                if (_olaWeight[i] > 1e-12)
                {
                    last = i;
                    break;
                }
            }

            if (last >= 0)
                EmitFrames(last + 1);

            _tailFlushed = true;
        }

        protected static double WrapPhase(double value)
        {
            value %= 2.0 * Math.PI;

            if (value > Math.PI)
                value -= 2.0 * Math.PI;

            else if (value < -Math.PI)
                value += 2.0 * Math.PI;

            return value;
        }

        protected void AddOutput(double value)
        {
            value = Math.Clamp(value, -1.0, 1.0);

            _output.Add(
                (short)Math.Round(value * 32767.0));
        }

        protected void EnsureConfigured()
        {
            if (!_configured)
                throw new InvalidOperationException("The stretcher is not configured.");
        }

        protected void DisposeFft()
        {
            if (!_configured)
                return;

            _forwardPlan.Dispose();
            _inversePlan.Dispose();

            _fftIn?.Dispose();
            _fftOut?.Dispose();

            _fftIn = null;
            _fftOut = null;

            _configured = false;
        }

        public void Dispose()
        {
            DisposeFft();
        }

        public bool IsDrained =>
            _flushed &&
            _tailFlushed &&
            _outputOffset >= _output.Count;

        public int FFTSize
        {
            get => _fftSize;
            set
            {
                if (_configured)
                    throw new InvalidOperationException("FFT size cannot be changed after configuration.");

                if (value < 256 || (value & (value - 1)) != 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _fftSize = value;
            }
        }
    }
}