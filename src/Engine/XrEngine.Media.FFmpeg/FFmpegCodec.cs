using FFmpeg.AutoGen;
using Silk.NET.Direct3D11;
using Silk.NET.WGL.Extensions.NV;
using Silk.NET.WGL;
using System.Runtime.InteropServices;
using XrEngine.Video.Abstraction;
using static FFmpeg.AutoGen.ffmpeg;
using XrEngine.OpenGL;
using System.Xml.Linq;



namespace XrEngine.Video
{
    public unsafe class FFmpegCodec : IVideoCodec
    {
        static readonly byte[] _emptyArray = [];

        private AVFrame* _pReceivedFrame = null;
        private int _streamIndex = 0;
        private AVCodecContext* _pCodecContext = null;
        private AVPacket* _pPacket = null;
        private AVFrame* _pFrame = null;
        private VideoFormat _outFormat;
        private SwsContext* _swsContext;

        /*
        Silk.NET.Direct3D11.ID3D11Texture2D* _dxText = null;
        private Texture2DDesc _dxTexDesc;
        private nint _hDxDevice;
        private nint _hDxObject;
        private NVDXInterop _nvExt;
        private bool _isLocked;

        enum D3D11_BIND_FLAG
        {
            D3D11_BIND_VERTEX_BUFFER = 0x1,
            D3D11_BIND_INDEX_BUFFER = 0x2,
            D3D11_BIND_CONSTANT_BUFFER = 0x4,
            D3D11_BIND_SHADER_RESOURCE = 0x8,
            D3D11_BIND_STREAM_OUTPUT = 0x10,
            D3D11_BIND_RENDER_TARGET = 0x20,
            D3D11_BIND_DEPTH_STENCIL = 0x40,
            D3D11_BIND_UNORDERED_ACCESS = 0x80,
            D3D11_BIND_DECODER = 0x200,
            D3D11_BIND_VIDEO_ENCODER = 0x400
        };


        */

        public FFmpegCodec()
        {
            ffmpeg.RootPath = "D:\\Development\\Library\\ffmpeg-full-win64\\bin\\";
            DeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA;
        }

        static unsafe string av_strerror(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer)!;
            return message;
        }

        static int CheckResult(int result, string msg)
        {
            if (result < 0)
                throw new ApplicationException(av_strerror(result));
            return result;
        }


        public void Dispose()
        {
            var pRecFrame = _pReceivedFrame;
            av_frame_free(&pRecFrame);

            var pFrame = _pFrame;
            av_frame_free(&pFrame);

            var pPacket = _pPacket;
            av_packet_free(&pPacket);

            var pCodecContext = _pCodecContext;
            avcodec_free_context(&pCodecContext);

            GC.SuppressFinalize(this);
        }

        private static AVPixelFormat GetHWPixelFormat(AVHWDeviceType hWDevice)
        {

            return hWDevice switch
            {
                AVHWDeviceType.AV_HWDEVICE_TYPE_NONE => AVPixelFormat.AV_PIX_FMT_NONE,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU => AVPixelFormat.AV_PIX_FMT_VDPAU,
                AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA => AVPixelFormat.AV_PIX_FMT_CUDA,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI => AVPixelFormat.AV_PIX_FMT_VAAPI,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2 => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_QSV => AVPixelFormat.AV_PIX_FMT_QSV,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX => AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX,
                AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DRM => AVPixelFormat.AV_PIX_FMT_DRM_PRIME,
                AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL => AVPixelFormat.AV_PIX_FMT_OPENCL,
                AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC => AVPixelFormat.AV_PIX_FMT_MEDIACODEC,
                _ => AVPixelFormat.AV_PIX_FMT_NONE
            };
        }

        public void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat, byte[]? extraData = null)
        {
            if (_pCodecContext != null)
            {
                var pCodecContext = _pCodecContext;
                avcodec_free_context(&pCodecContext);
            }

            var codecId = mimeType switch
            {
                "video/avc" => AVCodecID.AV_CODEC_ID_H264,
                _ => throw new NotSupportedException()
            };

            var codec = mode == VideoCodecMode.Decode ? avcodec_find_decoder(codecId) : avcodec_find_encoder(codecId);

            _pCodecContext = avcodec_alloc_context3(codec);
  
            _pCodecContext->width = outFormat.Width;
            _pCodecContext->height = outFormat.Height;
            if (extraData != null)
            {
                _pCodecContext->extradata_size = extraData.Length;
                _pCodecContext->extradata = (byte*)av_malloc((ulong)_pCodecContext->extradata_size);
                Marshal.Copy(extraData, 0, (IntPtr)(_pCodecContext->extradata), extraData.Length);
            }   
  
            AVPixelFormat outPixelFormat;

            if (DeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                CheckResult(av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, DeviceType, null, null, 0), "");
                outPixelFormat = GetHWPixelFormat(DeviceType);
            }
            else
            {
                var none = AVPixelFormat.AV_PIX_FMT_NONE;
                outPixelFormat = avcodec_default_get_format(_pCodecContext, &none);
            }

            CheckResult(avcodec_open2(_pCodecContext, codec, null), "");

            _pReceivedFrame = av_frame_alloc();
            _pPacket = av_packet_alloc();
            _pFrame = av_frame_alloc();

            _outFormat = outFormat;

            _swsContext = sws_getContext(
                _outFormat.Width, _outFormat.Height, outPixelFormat,
                _outFormat.Width, _outFormat.Height, AVPixelFormat.AV_PIX_FMT_RGBA,
                SWS_BILINEAR, null, null, null);


            if (_outFormat.ImageFormat != ImageFormat.Rgb32)
                throw new NotSupportedException();
        }

        public bool EnqueueBuffer(FrameBuffer src)
        {
            _pPacket->pts = 0;

            fixed (byte* pSrc = src.ByteArray ?? _emptyArray)
            {
                if (src.ByteArray != null)
                {
                    _pPacket->data = pSrc + src.Offset;
                    _pPacket->size = src.Size == 0 ? src.ByteArray.Length : src.Size;
                }
                else
                {
                    _pPacket->data = ((byte*)src.Pointer.ToPointer()) + src.Offset;
                    _pPacket->size = src.Size;
                }
               

                try
                {
                    lock (this)
                        avcodec_send_packet(_pCodecContext, _pPacket);
                }
                finally
                {
                    av_packet_unref(_pPacket);
                }
            }

            return true;
        }

        public bool DequeueBuffer(ref FrameBuffer dst)
        {
            int result;
            var frame = _pFrame;

            lock (this)
            {
                result = avcodec_receive_frame(_pCodecContext, _pFrame);

                if (result != 0)
                    return false;
                /*
                
                AVBufferRef* hw_device_ctx_buffer = _pCodecContext->hw_device_ctx;
                AVHWDeviceContext* hw_device_ctx = (AVHWDeviceContext*)hw_device_ctx_buffer->data;
                AVD3D11VADeviceContext* hw_d3d11_dev_ctx = (AVD3D11VADeviceContext*)hw_device_ctx->hwctx;

                Silk.NET.Direct3D11.ID3D11Texture2D* texture = (Silk.NET.Direct3D11.ID3D11Texture2D*)(void*)_pFrame->data[0];
                var arrayIndex = (int)(nint)_pFrame->data[1];

                if (_dxText == null)
                {
                    OpenGLRender.Current!.Dispatcher.ExecuteAsync(() =>
                    {
                        _dxTexDesc = new Texture2DDesc();

                        texture->GetDesc(ref _dxTexDesc);

                        _dxTexDesc.Format = Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm;
                        _dxTexDesc.BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_DECODER);
                        _dxTexDesc.ArraySize = 1;
                        _dxTexDesc.MiscFlags = 0;

                        ((Silk.NET.Direct3D11.ID3D11Device*)hw_d3d11_dev_ctx->device)->CreateTexture2D(ref _dxTexDesc, null, ref _dxText);

                        _nvExt = new NVDXInterop(OpenGLRender.Current.GL.Context);

                        var glText = OutTexture!.GetProp<GlTexture>(OpenGLRender.Props.GlResId)!;

                        _hDxDevice = _nvExt.DxopenDevice(hw_d3d11_dev_ctx->device);
                        _hDxObject = _nvExt.DxregisterObject(_hDxDevice, _dxText, glText.Handle, (NV)0x0DE1, NV.AccessReadWriteNV);
                    });

                    return false;
                }
                else
                {
                    OpenGLRender.Current!.Dispatcher.ExecuteAsync(() =>
                    {
                        var pDxObject = _hDxObject;

                        if (_isLocked)
                            _nvExt.DxunlockObjects(_hDxDevice, 1, &pDxObject);

                        _dxText->QueryInterface<ID3D11Resource>(out var dstTex);
                        texture->QueryInterface<ID3D11Resource>(out var srcTex);

                        var context = (Silk.NET.Direct3D11.ID3D11DeviceContext*)hw_d3d11_dev_ctx->device_context;

                        context->CopySubresourceRegion(dstTex.Handle, 0, 0, 0, 0, srcTex.Handle, (uint)arrayIndex, null);

                        _nvExt.DxlockObjects(_hDxDevice, 1, &pDxObject);
                        _isLocked = true;

                    });

                    return true;
                }
                      */
            }

            if (_pCodecContext->hw_device_ctx != null)
            {
                try
                {
                    CheckResult(av_hwframe_transfer_data(_pReceivedFrame, _pFrame, 0), "");
                }
                finally
                {
                    av_frame_unref(_pFrame);
                }

                frame = _pReceivedFrame;
            }
            else
                frame = _pFrame;

            if (_swsContext == null)
                return false;

            var size = _outFormat.Width * _outFormat.Height * 4;

            if (dst.ByteArray == null || dst.ByteArray.Length != size)
                dst.ByteArray = new byte[size];

            fixed (byte* pData = dst.ByteArray)
            {
                sws_scale(_swsContext, frame->data, frame->linesize, 0,
                     _pCodecContext->height, [pData], [_outFormat.Width * 4]);
            }

            av_frame_unref(frame);

            return true;
        }

        public AVHWDeviceType DeviceType { get; set; }

        public Texture2D? OutTexture { get; set; }

        public VideoCodecCaps Caps => VideoCodecCaps.None;
    }
}
