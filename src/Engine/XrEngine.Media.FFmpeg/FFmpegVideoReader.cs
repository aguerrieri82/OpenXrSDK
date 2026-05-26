using Common.Interop;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using XrMath;
using static FFmpeg.AutoGen.ffmpeg;

namespace XrEngine.Media.FFmpeg
{
    public unsafe class FFmpegVideoReader : IVideoReader
    {
        const int SWS_BILINEAR = 2;

        private AVFormatContext* _pFormatContext = null;
        private AVFrame* _receivedFrame = null;
        private int _streamIndex = 0;
        private AVCodecContext* _pCodecContext = null;
        private AVPacket* _pPacket = null;
        private AVFrame* _pFrame = null;
        private SwsContext* _swsContext;

        private struct FrameIndexEntry
        {
            public long Pts;
            public bool IsKeyFrame;
            public double Seconds;
        }

        private readonly List<FrameIndexEntry> _frameIndex = new();
        private bool _isIndexed = false;
        private TextureFormat _outFormat;

        public FFmpegVideoReader()
        {
            ffmpeg.RootPath = "D:\\Development\\Library\\ffmpeg-full-win64\\bin\\";
        }

        static string av_strerror(int error)
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
                throw new ApplicationException($"{msg}: {av_strerror(result)}");
            return result;
        }

        public void Open(Uri source, TextureFormat outFormat = TextureFormat.Rgba32)
        {
            _outFormat = outFormat;

            if (!source.IsFile)
                throw new NotSupportedException();

            Open(source.LocalPath, AVHWDeviceType.AV_HWDEVICE_TYPE_NONE);
        }

        public void Open(string filename, AVHWDeviceType deviceType)
        {
            _pFormatContext = avformat_alloc_context();
            _receivedFrame = av_frame_alloc();
            var pFormatContext = _pFormatContext;

            CheckResult(avformat_open_input(&pFormatContext, filename, null, null), "Could not open input");
            CheckResult(avformat_find_stream_info(_pFormatContext, null), "Could not find stream info");

            AVCodec* codec = null;
            _streamIndex = CheckResult(av_find_best_stream(_pFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0), "Could not find video stream");

            _pCodecContext = avcodec_alloc_context3(codec);

            if (deviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                CheckResult(av_hwdevice_ctx_create(&_pCodecContext->hw_device_ctx, deviceType, null, null, 0), "HW device create failed");

            CheckResult(avcodec_parameters_to_context(_pCodecContext, _pFormatContext->streams[_streamIndex]->codecpar), "Codec parameters failed");
            CheckResult(avcodec_open2(_pCodecContext, codec, null), "Codec open failed");

            CodecName = avcodec_get_name(codec->id);
            FrameSize = new Size2I((uint)_pCodecContext->width, (uint)_pCodecContext->height);
            PixelFormat = _pCodecContext->pix_fmt;
            FrameRate = _pCodecContext->framerate.num / (double)_pCodecContext->framerate.den;

            _pPacket = av_packet_alloc();
            _pFrame = av_frame_alloc();

            var pixelFormat = _outFormat switch
            {
                TextureFormat.Rgba32 => AVPixelFormat.AV_PIX_FMT_RGBA,
                TextureFormat.Rgb24 => AVPixelFormat.AV_PIX_FMT_RGB24,
                _ => throw new NotImplementedException()
            };

            _swsContext = sws_getContext(
               _pCodecContext->width, _pCodecContext->height, _pCodecContext->pix_fmt,
               _pCodecContext->width, _pCodecContext->height, pixelFormat,
               SWS_BILINEAR, null, null, null);
        }

        public void Seek(double timeInSeconds)
        {
            if (_pFormatContext == null) return;

            var internalTimeBase = new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE };

            var timestampMicroseconds = (long)(timeInSeconds * ffmpeg.AV_TIME_BASE);

            var stream = _pFormatContext->streams[_streamIndex];
            var targetTimestamp = av_rescale_q(timestampMicroseconds, internalTimeBase, stream->time_base);

            CheckResult(av_seek_frame(_pFormatContext, _streamIndex, targetTimestamp, AVSEEK_FLAG_BACKWARD), "Seek failed");

            if (_pCodecContext != null)
                avcodec_flush_buffers(_pCodecContext);
        }

        public void Rewind()
        {
            Seek(0);
        }

        public void BuildFrameIndex()
        {
            if (_pFormatContext == null)
                return;

            Seek(0);

            _frameIndex.Clear();
            var tempPacket = av_packet_alloc();

            while (av_read_frame(_pFormatContext, tempPacket) >= 0)
            {
                if (tempPacket->stream_index == _streamIndex)
                {
                    var stream = _pFormatContext->streams[_streamIndex];

                    _frameIndex.Add(new FrameIndexEntry
                    {
                        Pts = tempPacket->pts,
                        Seconds = tempPacket->pts * av_q2d(stream->time_base),
                        IsKeyFrame = (tempPacket->flags & AV_PKT_FLAG_KEY) != 0
                    });
                }
                av_packet_unref(tempPacket);
            }

            av_packet_free(&tempPacket);
            _isIndexed = true;

            Seek(0);
        }

        public bool SeekToFrame(int targetFrameIndex)
        {
            if (!_isIndexed)
                BuildFrameIndex();

            if (targetFrameIndex < 0 || targetFrameIndex >= _frameIndex.Count)
                return false;
            var targetPts = _frameIndex[targetFrameIndex].Pts;

            var ret = av_seek_frame(_pFormatContext, _streamIndex, targetPts, AVSEEK_FLAG_BACKWARD);
            if (ret < 0)
                return false;

            avcodec_flush_buffers(_pCodecContext);

            AVFrame frameData;
            while (true)
            {
                if (!TryReadNextFrameInternal(out frameData))
                    return false;

                if (frameData.pts >= targetPts)
                    return true;
            }
        }

        public bool TryDecodeNextFrame(TextureData data)
        {
            if (!TryDecodeNextFrame(out var frame))
                return false;

            data.Width = (uint)_pCodecContext->width;
            data.Height = (uint)_pCodecContext->height;
            data.Format = _outFormat;

            var pixeSize = _outFormat.GetPixelSizeBit() / 8;

            var size = data.Width * data.Height * pixeSize;
            data.Data = MemoryBuffer.CreateOrResize(data.Data, size);

            using var pData = data.Data.MemoryLock();

            sws_scale(_swsContext, frame.data, frame.linesize, 0,
                 _pCodecContext->height, [pData], [(int)data.Width * (int)pixeSize]);

            return true;
        }

        public bool TryDecodeNextFrame(out AVFrame frame)
        {
            return TryReadNextFrameInternal(out frame);
        }

        private bool TryReadNextFrameInternal(out AVFrame frame)
        {
            av_frame_unref(_pFrame);
            av_frame_unref(_receivedFrame);

            int result;
            frame = new AVFrame();

            do
            {
                try
                {
                    do
                    {
                        av_packet_unref(_pPacket);
                        result = av_read_frame(_pFormatContext, _pPacket);

                        if (result == AVERROR_EOF)
                            return false;

                        CheckResult(result, "Read frame failed");

                    } while (_pPacket->stream_index != _streamIndex);

                    CheckResult(avcodec_send_packet(_pCodecContext, _pPacket), "Send packet failed");
                }
                finally
                {
                    av_packet_unref(_pPacket);
                }

                result = avcodec_receive_frame(_pCodecContext, _pFrame);

            } while (result == AVERROR(EAGAIN));

            CheckResult(result, "Receive frame failed");

            if (_pCodecContext->hw_device_ctx != null)
            {
                CheckResult(av_hwframe_transfer_data(_receivedFrame, _pFrame, 0), "HW Transfer failed");
                frame = *_receivedFrame;
            }
            else
            {
                frame = *_pFrame;
            }

            return true;
        }

        public void Close()
        {
            var pFrame = _pFrame;
            av_frame_free(&pFrame);

            var pPacket = _pPacket;
            av_packet_free(&pPacket);

            var pCodecContext = _pCodecContext;
            avcodec_free_context(&pCodecContext);

            var pFormatContext = _pFormatContext;
            avformat_close_input(&pFormatContext);

            _frameIndex.Clear();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public string? CodecName { get; private set; }
        public double FrameRate { get; private set; }
        public Size2I FrameSize { get; private set; }
        public AVPixelFormat PixelFormat { get; private set; }
        public Texture2D? OutTexture { get; set; }

        public bool IsIndexed => _isIndexed;

        public int FrameCount => _frameIndex.Count;
    }
}