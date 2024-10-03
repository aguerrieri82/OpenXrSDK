using Android.Graphics;
using Android.Media;
using Android.Views;
using XrEngine.OpenGL;
using XrEngine.Video;
using XrMath;



namespace XrEngine.Media.Android
{
    public class AndroidVideoReader : IVideoReader
    {
        private MediaCodec? _decoder;
        private MediaExtractor? _mediaExtractor;
        private readonly long _timeout;
        private bool _eos;
        private double _frameRate;
        private Size2I _frameSize;
        private bool _isDecoderInit;
        private MediaFormat? _inputFormat;
        private SurfaceTexture? _surfaceTex;

        public AndroidVideoReader()
        {
            _timeout = 1000000;
            IsLoop = true;
        }

        public void Close()
        {
            if (_decoder != null)
            {
                _decoder.Stop();
                _decoder.Release();
                _decoder.Dispose();
                _decoder = null;
            }

            if (_mediaExtractor != null)
            {
                _mediaExtractor.Release();
                _mediaExtractor.Dispose();
                _mediaExtractor = null;
            }

            if (_inputFormat != null)
            {
                _inputFormat.Dispose();
                _inputFormat = null;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public void Open(Uri source)
        {
            if (!source.IsFile)
                throw new NotSupportedException();

            _eos = false;
            _mediaExtractor = new MediaExtractor();
            _mediaExtractor.SetDataSource(source.LocalPath);

            var tracks = _mediaExtractor.TrackCount;

            string? mimeType = null;

            for (int i = 0; i < tracks; ++i)
            {
                var format = _mediaExtractor.GetTrackFormat(i);

                mimeType = format.GetString(MediaFormat.KeyMime);

                if (mimeType != null && mimeType.StartsWith("video/"))
                {
                    _inputFormat = format;

                    _frameSize.Width = (uint)_inputFormat.GetInteger(MediaFormat.KeyWidth);
                    _frameSize.Height = (uint)_inputFormat.GetInteger(MediaFormat.KeyHeight);
                    _frameRate = _inputFormat.GetInteger(MediaFormat.KeyFrameRate);

                    _mediaExtractor.SelectTrack(i);

                    _inputFormat.SetInteger(MediaFormat.KeyCaptureRate, (int)_frameRate);
                    _inputFormat.SetInteger(MediaFormat.KeyPushBlankBuffersOnStop, 1);

                    break;
                }

            }

            if (_inputFormat == null)
                throw new NotSupportedException();

            _decoder = MediaCodec.CreateDecoderByType(mimeType!);
            _isDecoderInit = false;
        }

        public bool TryDecodeNextFrame(TextureData data)
        {

            if (_decoder == null || _mediaExtractor == null || _inputFormat == null)
                throw new InvalidOperationException();

            if (!_isDecoderInit)
            {
                //var caps = _decoder.CodecInfo.GetCapabilitiesForType(mimeType!);

                Surface? surface = null;

                if (OutTexture != null)
                {
                    var glText = OutTexture!.GetProp<GlTexture>(OpenGLRender.Props.GlResId);
                    if (glText == null)
                        return false;

                    _surfaceTex = new SurfaceTexture((int)glText.Handle);
                    surface = new Surface(_surfaceTex);
                }

                _decoder.Configure(_inputFormat, surface, null, MediaCodecConfigFlags.None);
                _decoder.Start();
                _isDecoderInit = true;
            }

            if (_eos)
                return false;

            var inBufferIndex = _decoder.DequeueInputBuffer(_timeout);

            if (inBufferIndex < 0)
                return false;

            var inputBuffer = _decoder.GetInputBuffer(inBufferIndex);

            var sampleSize = _mediaExtractor.ReadSampleData(inputBuffer!, 0);

            if (sampleSize > 0)
            {
                _decoder.QueueInputBuffer(inBufferIndex, 0, sampleSize, _mediaExtractor.SampleTime, MediaCodecBufferFlags.None);
                _mediaExtractor.Advance();
            }
            else
            {
                if (IsLoop)
                {
                    _mediaExtractor.SeekTo(0, MediaExtractorSeekTo.None);
                    return false;
                }

                _decoder.QueueInputBuffer(inBufferIndex, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                _eos = true;
            }

            var bufferInfo = new MediaCodec.BufferInfo();

            var outBufferIndex = _decoder.DequeueOutputBuffer(bufferInfo, _timeout);

            if (outBufferIndex > 0)
            {
                _decoder.ReleaseOutputBuffer(outBufferIndex, true);

                if (_surfaceTex != null)
                {
                    _surfaceTex.UpdateTexImage();
                }

                return true;
            }

            return false;
        }

        public bool IsLoop { get; set; }

        public Texture2D? OutTexture { get; set; }

        public double FrameRate => _frameRate;

        public Size2I FrameSize => _frameSize;

    }
}
