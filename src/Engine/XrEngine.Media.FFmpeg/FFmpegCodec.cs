using FFmpeg.AutoGen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using XrEngine.Video.Abstraction;
using static FFmpeg.AutoGen.ffmpeg;



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

        public FFmpegCodec()
        {
            ffmpeg.RootPath = "D:\\Development\\Library\\ffmpeg-full-win64\\bin\\";
            DeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
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

        public void Open(VideoCodecMode mode, string mimeType, VideoFormat outFormat)
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

            AVPixelFormat outPixeFormat;

            if (DeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                CheckResult(av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, DeviceType, null, null, 0), "");
                outPixeFormat = GetHWPixelFormat(DeviceType);
            }
            else
            {
                var none = AVPixelFormat.AV_PIX_FMT_NONE;
                outPixeFormat = avcodec_default_get_format(_pCodecContext, &none);
            }

            CheckResult(avcodec_open2(_pCodecContext, codec, null), "");

            _pReceivedFrame = av_frame_alloc();
            _pPacket = av_packet_alloc();
            _pFrame = av_frame_alloc();

            _outFormat = outFormat;


            _swsContext = sws_getContext(
                _outFormat.Width, _outFormat.Height, (AVPixelFormat)outPixeFormat,
                _outFormat.Width, _outFormat.Height, AVPixelFormat.AV_PIX_FMT_RGBA,
                SWS_BILINEAR, null, null, null);


            if (_outFormat.ImageFormat != ImageFormat.Rgb32)
                throw new NotSupportedException();
        }

        public bool Convert(FrameBuffer src, ref FrameBuffer dst)
        {
            int result;
            var frame = _pFrame;

            av_packet_unref(_pPacket);
            av_frame_unref(_pFrame);
            av_frame_unref(_pReceivedFrame);

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
                    CheckResult(avcodec_send_packet(_pCodecContext, _pPacket), "");
                }
                finally
                {
                    av_packet_unref(_pPacket);
                }

                result = avcodec_receive_frame(_pCodecContext, _pFrame);

                if (result != 0)
                    return false;
            }

            CheckResult(result, "");


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
