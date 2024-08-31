using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using static FFmpeg.AutoGen.ffmpeg;


namespace XrEngine.Video
{
    public unsafe class VideoDecoder : IDisposable  
    {

        private AVFormatContext* _pFormatContext = null;
        private AVFrame* _receivedFrame = null;
        private int _streamIndex = 0;
        private AVCodecContext* _pCodecContext = null;
        private AVPacket* _pPacket = null;
        private AVFrame* _pFrame = null;
        private SwsContext* _swsContext;

        public VideoDecoder()
        {

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

        public unsafe void Open(string filename, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {

            _pFormatContext = avformat_alloc_context();
            _receivedFrame = av_frame_alloc();
            var pFormatContext = _pFormatContext;
            CheckResult(avformat_open_input(&pFormatContext, filename, null, null), "");
            CheckResult(avformat_find_stream_info(_pFormatContext, null), "");
            AVCodec* codec = null;
            _streamIndex = CheckResult(av_find_best_stream(_pFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0), "");

            _pCodecContext = avcodec_alloc_context3(codec);

            if (deviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                CheckResult(av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, deviceType, null, null, 0), "");

            CheckResult(avcodec_parameters_to_context(_pCodecContext, _pFormatContext->streams[_streamIndex]->codecpar), "");

            CheckResult(avcodec_open2(_pCodecContext, codec, null), "");

            CodecName = avcodec_get_name(codec->id);
            FrameSize = new Size(_pCodecContext->width, _pCodecContext->height);
            PixelFormat = _pCodecContext->pix_fmt;
            FrameRate = _pCodecContext->framerate.num / (double)_pCodecContext->framerate.den;

            _pPacket = av_packet_alloc();
            _pFrame = av_frame_alloc();

            _swsContext = sws_getContext(
               _pCodecContext->width, _pCodecContext->height, _pCodecContext->pix_fmt,
               _pCodecContext->width, _pCodecContext->height, AVPixelFormat.AV_PIX_FMT_RGBA,
               SWS_BILINEAR, null, null, null);
        }

        public bool TryDecodeNextFrame(TextureData data)
        {
            if (!TryDecodeNextFrame(out var frame))
                return false;
            
            data.Width = (uint)_pCodecContext->width;
            data.Height = (uint)_pCodecContext->height;
            data.Format = TextureFormat.Rgba32;

            var size = data.Width * data.Height * 4;

            if (data.Data.Length != size)
                data.Data = new Memory<byte>(new byte[size]);

            fixed (byte* pData = data.Data.Span)
            {
                sws_scale(_swsContext, frame.data, frame.linesize, 0,
                     _pCodecContext->height, [pData], [(int)data.Width * 4]);

            }

            return true;
        }

        public bool TryDecodeNextFrame(out AVFrame frame)
        {
            av_frame_unref(_pFrame);
            av_frame_unref(_receivedFrame);
            int result;

            do
            {
                try
                {
                    do
                    {
                        av_packet_unref(_pPacket);
                        result = av_read_frame(_pFormatContext, _pPacket);

                        if (result == AVERROR_EOF)
                        {
                            frame = *_pFrame;
                            return false;
                        }

                        CheckResult(result, "");

                    } while (_pPacket->stream_index != _streamIndex);

                    CheckResult(avcodec_send_packet(_pCodecContext, _pPacket), "");
                }
                finally
                {
                    av_packet_unref(_pPacket);
                }

                result = avcodec_receive_frame(_pCodecContext, _pFrame);

            } while (result == AVERROR(EAGAIN));

            CheckResult(result, "");

            if (_pCodecContext->hw_device_ctx != null)
            {
                CheckResult(av_hwframe_transfer_data(_receivedFrame, _pFrame, 0), "");
                frame = *_receivedFrame;
            }
            else
                frame = *_pFrame;

            return true;
        }

        public void Dispose()
        {
            var pFrame = _pFrame;
            av_frame_free(&pFrame);

            var pPacket = _pPacket;
            av_packet_free(&pPacket);

            var pCodecContext = _pCodecContext;
            avcodec_free_context(&pCodecContext);

            var pFormatContext = _pFormatContext;
            avformat_close_input(&pFormatContext);

            GC.SuppressFinalize(this);
        }

        public string? CodecName { get; private set; }

        public double FrameRate { get; private set; }

        public Size FrameSize { get; private set; }

        public AVPixelFormat PixelFormat { get; private set; }

    }
}
