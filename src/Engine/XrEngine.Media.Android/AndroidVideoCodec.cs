using Android.Graphics;
using Android.Media;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Silk.NET.OpenGLES;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XrEngine.OpenGL;
using XrEngine.Video;
using XrEngine.Video.Abstraction;
using static Android.Icu.Text.TimeZoneFormat;


namespace XrEngine.Media.Android
{

    public class AndroidVideoCodec : IVideoCodec
    {
        private MediaCodec? _codec;
        private VideoFormat _outFormat;
        private string? _mimeType;
        private bool _isCodecInit;
        private SurfaceTexture? _surfaceTex;
        private long _timeout;

        public AndroidVideoCodec()
        {
            _timeout = 1000000;
        }

        public bool Convert(FrameBuffer src, ref FrameBuffer dst)
        {
            if (_codec == null)
                return false;

            if (!_isCodecInit)
            {
                Surface? surface = null;

                if (OutTexture != null)
                {
                    var glText = OutTexture!.GetProp<GlTexture>(OpenGLRender.Props.GlResId);
                    if (glText != null)
                    {
                        _surfaceTex = new SurfaceTexture((int)glText.Handle);

                        surface = new Surface(_surfaceTex);
                    }
                }

                var inFormat = MediaFormat.CreateVideoFormat(_mimeType!, _outFormat.Width, _outFormat.Height);

                _codec.Configure(inFormat, surface, null, MediaCodecConfigFlags.None);
                _codec.Start();

                _isCodecInit = true;
            }


            var inBufferIndex = _codec.DequeueInputBuffer(_timeout);

            if (inBufferIndex < 0)
                return false;

            var inputBuffer = _codec.GetInputBuffer(inBufferIndex)!;

            var size = src.Size == 0 ? src.ByteArray.Length : src.Size;

            inputBuffer.Clear();
            inputBuffer.Put(src.ByteArray, src.Offset, size);

            _codec.QueueInputBuffer(inBufferIndex, 0, size, 0, MediaCodecBufferFlags.None);

            var bufferInfo = new MediaCodec.BufferInfo();

            var outBufferIndex = _codec.DequeueOutputBuffer(bufferInfo, _timeout);

            if (outBufferIndex > 0)
            {
                _codec.ReleaseOutputBuffer(outBufferIndex, true);

                if (_surfaceTex != null)
                {
                    _surfaceTex.UpdateTexImage();
                }

                return true;
            }

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

        public void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat)
        {
            _codec = mode == VideoCodecMode.Decode ?
                MediaCodec.CreateDecoderByType(mimeType) :
                MediaCodec.CreateEncoderByType(mimeType);

            _outFormat = outFormat;
            _mimeType = mimeType;
        }

        public Texture2D? OutTexture { get; set; }

        public VideoCodecCaps Caps => VideoCodecCaps.DecodeTexture;
    }
}
