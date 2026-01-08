using Android.Media;
using Android.Views;
using System.Diagnostics;

namespace XrEngine.Media.Android
{


    public class AndroidVideoRecorder : IVideoRecorder
    {

        class MediaCodecCallback : MediaCodec.Callback
        {
            private readonly AndroidVideoRecorder _host;

            public MediaCodecCallback(AndroidVideoRecorder host)
            {
                _host = host;
            }

            public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
            {

            }

            public override void OnInputBufferAvailable(MediaCodec codec, int index)
            {

            }

            public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
            {

            }

            public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
            {

            }
        }


        private MediaCodec? _codec;
        private MediaMuxer? _muxer;
        private bool _muxerStarted;
        private int _trackIndex;
        private Surface? _surface;

        public NativeSurface StartRecording(string outPath, VideoRecordOptions options)
        {
            var format = MediaFormat.CreateVideoFormat("video/avc", options.Width, options.Height);
            format.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
            format.SetInteger(MediaFormat.KeyBitRate, options.BitRate);
            format.SetInteger(MediaFormat.KeyFrameRate, options.FrameRate);
            format.SetInteger(MediaFormat.KeyIFrameInterval, options.IFrameInterval);

            _codec = MediaCodec.CreateEncoderByType("video/avc");
            //_codec.SetCallback(new MediaCodecCallback(this));
            _codec.Configure(format, null, null, MediaCodecConfigFlags.Encode);

            _surface = _codec.CreateInputSurface();

            _codec.Start();

            var outType = options.Format switch
            {
                VideoRecordFormat.Mp4 => MuxerOutputType.Mpeg4,
                _ => throw new NotImplementedException()
            };

            _muxer = new MediaMuxer(outPath, outType);

            return new NativeSurface { Native = _surface };
        }

        public bool ProcessEncodedFrames(out long timestamp)
        {
            Debug.Assert(_muxer != null);
            Debug.Assert(_codec != null);

            var bufferInfo = new MediaCodec.BufferInfo();

            timestamp = 0;

            var encoderStatus = _codec.DequeueOutputBuffer(bufferInfo, 0);

            if (encoderStatus == (int)MediaCodecInfoState.TryAgainLater)
                return false;

            if (encoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
            {
                if (_muxerStarted)
                    throw new Exception("Format changed twice!");

                var newFormat = _codec.OutputFormat;
                _trackIndex = _muxer.AddTrack(newFormat);
                _muxer.Start();
                _muxerStarted = true;
                return false;
            }

            if (encoderStatus >= 0)
            {
                var encodedData = _codec.GetOutputBuffer(encoderStatus)!;

                if ((bufferInfo.Flags & MediaCodecBufferFlags.CodecConfig) != 0)
                    bufferInfo.Size = 0;

                if (bufferInfo.Size != 0)
                {
                    if (!_muxerStarted)
                        throw new Exception("Muxer hasn't started yet!");

                    timestamp = bufferInfo.PresentationTimeUs;

                    encodedData.Position(bufferInfo.Offset);
                    encodedData.Limit(bufferInfo.Offset + bufferInfo.Size);

                    _muxer.WriteSampleData(_trackIndex, encodedData, bufferInfo);
                }

                _codec.ReleaseOutputBuffer(encoderStatus, false);

                return true;
            }

            return false;
        }

        public void StopRecording()
        {
            if (_codec != null)
            {
                _codec.Stop();
                _codec.Release();
            }

            if (_muxer != null)
            {
                if (_muxerStarted)
                    _muxer.Stop();
                _muxer.Release();
            }
        }
    }
}
