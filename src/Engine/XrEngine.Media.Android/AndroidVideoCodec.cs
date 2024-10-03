using Android.Graphics;
using Android.Media;
using Android.Views;
using System.Collections.Concurrent;
using XrEngine.OpenGL;
using XrEngine.Video;
using XrEngine.Video.Abstraction;


namespace XrEngine.Media.Android
{

    public class AndroidVideoCodec : IVideoCodec
    {
        struct ConvertData
        {
            public FrameBuffer Src;
            public FrameBuffer Dst;
        }

        private MediaCodec? _codec;
        private VideoFormat _outFormat;
        private string? _mimeType;
        private bool _isCodecInit;
        private SurfaceTexture? _surfaceTex;
        private readonly long _timeout;
        private readonly ConcurrentQueue<ConvertData> _convertQueue = new ConcurrentQueue<ConvertData>();

        public AndroidVideoCodec()
        {
            _timeout = 60 * 1000;
        }

        protected bool EnsureCodecInit()
        {
            if (_codec == null)
                return false;

            if (_isCodecInit)
                return true;

            Surface? surface = null;

            if (OutTexture != null)
            {
                var glText = OutTexture!.GetProp<GlTexture>(OpenGLRender.Props.GlResId);

                if (glText == null)
                    return false;

                _surfaceTex = new SurfaceTexture((int)glText.Handle);
                surface = new Surface(_surfaceTex);
            }

            var inFormat = MediaFormat.CreateVideoFormat(_mimeType!, _outFormat.Width, _outFormat.Height);

            _codec.Configure(inFormat, surface, null, MediaCodecConfigFlags.None);
            _codec.Start();

            _isCodecInit = true;

            return true;

        }

        public bool EnqueueBuffer(FrameBuffer src)
        {
            if (!EnsureCodecInit())
                return false;

            var inBufferIndex = _codec!.DequeueInputBuffer(_timeout);

            if (inBufferIndex < 0)
            {
                Log.Warn(this, "EnqueueBuffer failed");
                return false;
            }


            var inputBuffer = _codec.GetInputBuffer(inBufferIndex)!;

            var size = src.Size == 0 ? src.ByteArray.Length : src.Size;

            inputBuffer.Clear();
            inputBuffer.Put(src.ByteArray, src.Offset, size);

            lock (this)
                _codec.QueueInputBuffer(inBufferIndex, 0, size, 0, MediaCodecBufferFlags.None);

            return true;
        }

        public bool DequeueBuffer(ref FrameBuffer dst)
        {
            if (!_isCodecInit)
                return false;

            var bufferInfo = new MediaCodec.BufferInfo();

            int outBufferIndex;

            lock (this)
                outBufferIndex = _codec!.DequeueOutputBuffer(bufferInfo, 0);

            if (outBufferIndex > 0)
            {
                _codec.ReleaseOutputBuffer(outBufferIndex, true);

                _surfaceTex?.UpdateTexImage();

                return true;
            }

            //Log.Warn(this, "DequeueBuffer failed");

            return false;
        }

        public void Dispose()
        {
            if (_codec != null)
            {
                _codec.Stop();
                _codec.Release();
                _codec.Dispose();
                _codec = null;
            }
        }

        public void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat, byte[]? extraData = null)
        {
            _codec = mode == VideoCodecMode.Decode ?
                MediaCodec.CreateDecoderByType(mimeType) :
                MediaCodec.CreateEncoderByType(mimeType);

            _outFormat = outFormat;
            _mimeType = mimeType;
            _convertQueue.Clear();

        }

        public Texture2D? OutTexture { get; set; }

        public VideoCodecCaps Caps => VideoCodecCaps.DecodeTexture;
    }
}
