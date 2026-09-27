using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using XrEngine.Media;

namespace XrEngine.Music
{
    public class WaveAudioTrack : AudioTrack
    {
        protected AudioFormat? _format;
        protected int _sampleIndex;
        protected IAudioTimeStretcher _timeStretcher;

        private int _queueStartFrame;
        private float _playStartPosition;
        private float _timeScale;
        private bool _stretcherConfigured;
        private bool _stretcherFlushed;

        protected byte[][] _buffers;
        protected byte[]?[] _activeBuffers;

        private DateTime _lastLogTime;
        private double _lastLatency;

        public WaveAudioTrack() : base(AudioTrackType.Wave)
        {
            var bufSize = 1024 * 8;

            _buffers = [new byte[bufSize], new byte[bufSize]];
            _activeBuffers = new byte[2][];
            _timeStretcher = new PhaseVocoderTimeStretcher();

            _sampleIndex = -1;
            _timeScale = 1f;
        }

        public AudioFormat? Format
        {
            get => _format;
            set
            {
                _format = value;
                _stretcherConfigured = false;
            }
        }

        public IAudioTimeStretcher TimeStretcher
        {
            get => _timeStretcher;
            set
            {
                _timeStretcher.Dispose();
                _timeStretcher = value;
                _stretcherConfigured = false;
            }
        }

        internal override void SetPosition(float value)
        {
            _sampleIndex = -1;
            _activeBlock = null;

            _timeStretcher.Reset();

            base.SetPosition(value);
        }

        protected override void StopPlay()
        {
            _sequencer?.MainOut?.Reset();

            _sampleIndex = -1;
            _activeBlock = null;

            _timeStretcher.Reset();
        }

        protected override void PlayWork()
        {

            Debug.Assert(_sequencer?.MainOut != null);
            Debug.Assert(_format != null);

            var pos = Math.Max(0, _sequencer.Position);
            var mainOut = _sequencer.MainOut;
            var timeScale = _sequencer.TimeScale;

            if (_activeBlock == null ||
                pos < _activeBlock.Start ||
                pos >= _activeBlock.End ||
                timeScale != _timeScale)
            {
                _activeBlock = _blocks.FirstOrDefault(a => pos >= a.Start && pos < a.End);
                _sampleIndex = -1;
            }

            if (_activeBlock == null)
            {
                Stop();
                return;
            }

            var bufCount = 0;

            if (_sampleIndex == -1)
            {
                Prepare(pos, timeScale);

                _activeBuffers[0] = _buffers[0];
                _activeBuffers[1] = _buffers[1];

                bufCount = 2;

                mainOut.Reset();
            }
            else
            {
                var nextBuf = mainOut.Dequeue(1);

                if (nextBuf != null)
                {
                    _queueStartFrame += nextBuf.Length / BytesPerFrame;

                    _activeBuffers[0] = nextBuf;
                    bufCount = 1;
                }
            }

            if (bufCount == 0)
                return;

            var data = ((WaveAudioBlock)_activeBlock).Data;

            for (var i = 0; i < bufCount; i++)
            {
                var buffer = _activeBuffers[i];

                Debug.Assert(buffer != null);

                FillBuffer(data, buffer);

                mainOut.Enqueue(buffer);
            }

            _sequencer.AdjustPosition(PlayPosition);
        }

        protected void Prepare(float position, float timeScale)
        {
            Debug.Assert(_format != null);
            Debug.Assert(_activeBlock != null);

            if (_format.SampleType != AudioSampleType.Short)
                throw new NotSupportedException("Time stretching requires 16-bit PCM.");

            if (!_stretcherConfigured)
            {
                _timeStretcher.Configure(_format);
                _stretcherConfigured = true;
            }
            else
                _timeStretcher.Reset();

            _timeStretcher.SetTempo(timeScale);

            _timeScale = timeScale;
            _playStartPosition = position;
            _queueStartFrame = 0;
            _stretcherFlushed = false;

            var relPos = position - _activeBlock.Start;
            var frame = (int)(relPos * _format.SampleRate);

            _sampleIndex = frame * BytesPerFrame;
        }

        protected void FillBuffer(byte[] data, byte[] buffer)
        {
            if (_timeScale == 1f)
            {
                buffer.AsSpan().Clear();

                if (_sampleIndex >= data.Length)
                    return;

                var count = Math.Min(buffer.Length, data.Length - _sampleIndex);

                count -= count % BytesPerFrame;

                if (count <= 0)
                {
                    _sampleIndex = data.Length;
                    return;
                }

                Buffer.BlockCopy(data, _sampleIndex, buffer, 0, count);

                _sampleIndex += count;

                return;
            }

            var output = MemoryMarshal.Cast<byte, short>(buffer.AsSpan());
            var written = 0;

            while (written < output.Length)
            {
                written += _timeStretcher.Read(output[written..]);

                if (written == output.Length)
                    break;

                if (_sampleIndex < data.Length)
                {
                    var remaining = data.Length - _sampleIndex;
                    var feedBytes = Math.Min(remaining, buffer.Length);

                    feedBytes -= feedBytes % BytesPerFrame;

                    if (feedBytes > 0)
                    {
                        var input = MemoryMarshal.Cast<byte, short>(data.AsSpan(_sampleIndex, feedBytes));

                        _timeStretcher.Write(input);

                        _sampleIndex += feedBytes;

                        continue;
                    }

                    _sampleIndex = data.Length;
                }

                if (!_stretcherFlushed)
                {
                    _timeStretcher.Flush();
                    _stretcherFlushed = true;
                    continue;
                }

                output[written..].Clear();

                break;
            }
        }

        public float PlayPosition
        {
            get
            {
                var output = (OpenAlAudioOut)_sequencer!.MainOut!;
                var ofsLat = output.Source.OffsetLatency;

                _lastLatency = ofsLat.Latency;

                var outputTime =
                    (_queueStartFrame + ofsLat.Offset) / _format!.SampleRate -
                    ofsLat.Latency;

                return _playStartPosition + (float)outputTime * _timeScale;
            }
        }

        protected int BytesPerFrame =>
            _format!.Channels * (_format.BitsPerSample / 8);

        public static WaveAudioTrack Load(string path, string? cachePath = null)
        {
            byte[] data;
            AudioFormat format;

            if (cachePath != null)
            {

                using var stream = File.OpenRead(path);
                var hash = Convert.ToHexString(SHA256.HashData(stream));

                var cacheFile = Path.Combine(cachePath, hash + ".pcm");
                var formatFile = Path.Combine(cachePath, hash + ".frm");

                if (File.Exists(cacheFile) && File.Exists(formatFile))
                {
                    data = File.ReadAllBytes(cacheFile);
                    format = JsonSerializer.Deserialize<AudioFormat>(File.ReadAllText(formatFile))!;
                }
                else
                {
                    var converter = Context.RequireNew<IAudioDecoder>();

                    data = converter.DecodeToPCM(path, out format);

                    Directory.CreateDirectory(cachePath);

                    File.WriteAllBytes(cacheFile, data);
                    File.WriteAllText(formatFile, JsonSerializer.Serialize(format));
                }
            }
            else
            {
                var converter = Context.RequireNew<IAudioDecoder>();
                data = converter.DecodeToPCM(path, out format);
            }

            var track = new WaveAudioTrack
            {
                Format = format
            };

            track.AddBlock(new WaveAudioBlock(data, format));

            return track;
        }
    }
}