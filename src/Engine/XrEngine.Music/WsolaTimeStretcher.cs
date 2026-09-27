
using XrEngine.Media;

namespace XrEngine.Music
{
    public class WsolaTimeStretcher : IAudioTimeStretcher
    {
        AudioFormat? _format;

        float[]? _input;
        float[]? _output;
        float[]? _overlap;
        float[]? _fade;

        int _inputRead;
        int _inputWrite;
        int _inputCount;

        int _outputRead;
        int _outputWrite;
        int _outputCount;

        int _windowFrames;
        int _overlapFrames;
        int _searchFrames;
        int _synthesisHop;

        double _analysisPosition;

        float _tempo;
        bool _started;
        bool _flushed;

        public WsolaTimeStretcher()
        {
            _tempo = 1f;
        }

        public void Configure(AudioFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);

            if (format.Channels <= 0 || format.SampleRate <= 0)
                throw new ArgumentException("Invalid audio format.", nameof(format));

            _format = format;

            _windowFrames = (int)(format.SampleRate * 0.040f);
            _overlapFrames = (int)(format.SampleRate * 0.020f);
            _searchFrames = (int)(format.SampleRate * 0.005f);

            _synthesisHop = _windowFrames - _overlapFrames;

            var channels = format.Channels;

            _input = new float[format.SampleRate * channels * 4];
            _output = new float[format.SampleRate * channels * 4];
            _overlap = new float[_overlapFrames * channels];
            _fade = new float[_overlapFrames];

            var den = _overlapFrames - 1f;

            for (var i = 0; i < _overlapFrames; i++)
            {
                var x = i / den;
                _fade[i] = 0.5f - 0.5f * MathF.Cos(MathF.PI * x);
            }

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
            _inputRead = 0;
            _inputWrite = 0;
            _inputCount = 0;

            _outputRead = 0;
            _outputWrite = 0;
            _outputCount = 0;

            _analysisPosition = 0;

            _started = false;
            _flushed = false;

            _overlap?.AsSpan().Clear();
        }

        public void Write(ReadOnlySpan<short> input)
        {
            EnsureConfigured();

            if (_flushed)
                throw new InvalidOperationException("The stretcher has already been flushed.");

            if (_inputCount + input.Length > _input!.Length)
                throw new InvalidOperationException("WSOLA input buffer overflow.");

            const float scale = 1f / 32768f;

            for (var i = 0; i < input.Length; i++)
            {
                _input[_inputWrite] = input[i] * scale;

                if (++_inputWrite == _input.Length)
                    _inputWrite = 0;
            }

            _inputCount += input.Length;

            Process();
        }

        public int Read(Span<short> output)
        {
            Process();

            var count = output.Length;

            if (count > _outputCount)
                count = _outputCount;

            var src = _output!;

            for (var i = 0; i < count; i++)
            {
                output[i] = (short)(src[_outputRead] * 32767f);

                if (++_outputRead == src.Length)
                    _outputRead = 0;
            }

            _outputCount -= count;

            return count;
        }

        public void Flush()
        {
            if (_flushed)
                return;

            _flushed = true;

            Process();

            if (_started)
            {
                EnsureOutputCapacity(_overlap!.Length);

                for (var i = 0; i < _overlap.Length; i++)
                    WriteOutput(_overlap[i]);

                _started = false;
            }
        }

        protected void Process()
        {
            if (_format == null)
                return;

            if (_tempo == 1f)
            {
                ProcessDirect();
                return;
            }

            var channels = _format.Channels;
            var inputFrames = _inputCount / channels;

            while (true)
            {
                if (!_started)
                {
                    if (inputFrames < _windowFrames)
                        return;

                    EnsureOutputCapacity(_synthesisHop * channels);

                    for (var frame = 0; frame < _synthesisHop; frame++)
                    {
                        var src = frame * channels;

                        for (var ch = 0; ch < channels; ch++)
                            WriteOutput(ReadInput(src + ch));
                    }

                    for (var frame = 0; frame < _overlapFrames; frame++)
                    {
                        var src = (_synthesisHop + frame) * channels;
                        var dst = frame * channels;

                        for (var ch = 0; ch < channels; ch++)
                            _overlap![dst + ch] = ReadInput(src + ch);
                    }

                    _analysisPosition = _synthesisHop * _tempo;
                    _started = true;

                    continue;
                }

                var expected = (int)Math.Round(_analysisPosition);

                if (expected + _searchFrames + _windowFrames > inputFrames)
                    return;

                var candidate = FindBestCandidate(expected);

                EmitCandidate(candidate);

                _analysisPosition += _synthesisHop * _tempo;

                var discardFrames = (int)_analysisPosition - _searchFrames;

                if (discardFrames > 0)
                {
                    ConsumeInput(discardFrames * channels);

                    _analysisPosition -= discardFrames;
                    inputFrames -= discardFrames;
                }
            }
        }

        protected int FindBestCandidate(int expected)
        {
            var min = expected - _searchFrames;
            var max = expected + _searchFrames;

            if (min < 0)
                min = 0;

            var channels = _format!.Channels;

            var best = expected;
            var bestScore = double.NegativeInfinity;
            var bestDistance = int.MaxValue;

            const double equalScoreTolerance = 0.0005;

            for (var candidate = min; candidate <= max; candidate++)
            {
                double dot = 0;
                double a2 = 0;
                double b2 = 0;

                var src = candidate * channels;

                for (var frame = 0; frame < _overlapFrames; frame++)
                {
                    var overlapBase = frame * channels;
                    var inputBase = src + frame * channels;

                    for (var ch = 0; ch < channels; ch++)
                    {
                        var a = _overlap![overlapBase + ch];
                        var b = ReadInput(inputBase + ch);

                        dot += a * b;
                        a2 += a * a;
                        b2 += b * b;
                    }
                }

                if (a2 < 1e-10 || b2 < 1e-10)
                    continue;

                var score = dot / Math.Sqrt(a2 * b2);
                var distance = candidate - expected;

                if (distance < 0)
                    distance = -distance;

                if (score > bestScore + equalScoreTolerance ||
                    score >= bestScore - equalScoreTolerance && distance < bestDistance)
                {
                    best = candidate;
                    bestScore = score;
                    bestDistance = distance;
                }
            }

            return best;
        }

        protected void EmitCandidate(int candidate)
        {
            var channels = _format!.Channels;

            EnsureOutputCapacity(_synthesisHop * channels);

            for (var frame = 0; frame < _overlapFrames; frame++)
            {
                var fadeIn = _fade![frame];
                var fadeOut = 1f - fadeIn;

                var src = (candidate + frame) * channels;
                var overlap = frame * channels;

                for (var ch = 0; ch < channels; ch++)
                {
                    WriteOutput(
                        _overlap![overlap + ch] * fadeOut +
                        ReadInput(src + ch) * fadeIn);
                }
            }

            for (var frame = _overlapFrames; frame < _synthesisHop; frame++)
            {
                var src = (candidate + frame) * channels;

                for (var ch = 0; ch < channels; ch++)
                    WriteOutput(ReadInput(src + ch));
            }

            for (var frame = 0; frame < _overlapFrames; frame++)
            {
                var src = (candidate + _synthesisHop + frame) * channels;
                var dst = frame * channels;

                for (var ch = 0; ch < channels; ch++)
                    _overlap![dst + ch] = ReadInput(src + ch);
            }
        }

        protected void ProcessDirect()
        {
            if (_inputCount == 0)
                return;

            EnsureOutputCapacity(_inputCount);

            while (_inputCount > 0)
            {
                WriteOutput(ReadInput(0));
                ConsumeInput(1);
            }
        }

        protected float ReadInput(int offset)
        {
            var index = _inputRead + offset;

            if (index >= _input!.Length)
                index %= _input.Length;

            return _input[index];
        }

        protected void ConsumeInput(int count)
        {
            _inputRead += count;

            if (_inputRead >= _input!.Length)
                _inputRead %= _input.Length;

            _inputCount -= count;
        }

        protected void WriteOutput(float value)
        {
            _output![_outputWrite] = value;

            if (++_outputWrite == _output.Length)
                _outputWrite = 0;

            _outputCount++;
        }

        protected void EnsureOutputCapacity(int additional)
        {
            if (_outputCount + additional > _output!.Length)
                throw new InvalidOperationException("WSOLA output buffer overflow.");
        }

        protected void EnsureConfigured()
        {
            if (_format == null)
                throw new InvalidOperationException("The stretcher is not configured.");
        }

        public void Dispose()
        {
        }

        public bool IsDrained =>
            _flushed &&
            !_started &&
            _outputCount == 0;
    }
}