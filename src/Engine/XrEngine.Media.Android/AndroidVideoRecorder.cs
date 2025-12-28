using Android.Media;
using Android.Views;
using System.Diagnostics;

namespace XrEngine.Media.Android
{


    public class AndroidVideoRecorder : IVideoRecorder
    {
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

        public long ProcessEncodedFrames()
        {

            Debug.Assert(_muxer != null);
            Debug.Assert(_codec != null);

            var bufferInfo = new MediaCodec.BufferInfo();

            long timestamp = 0;

            while (true)
            {
                // Check for available data
                var encoderStatus = _codec.DequeueOutputBuffer(bufferInfo, 0); // 0 = Don't wait

                if (encoderStatus == (int)MediaCodecInfoState.TryAgainLater)
                {
                    break;
                }
                else if (encoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    // CRITICAL: The encoder is ready. Start the Muxer now.
                    if (_muxerStarted)
                        throw new Exception("Format changed twice!");

                    var newFormat = _codec.OutputFormat;
                    _trackIndex = _muxer.AddTrack(newFormat);
                    _muxer.Start();
                    _muxerStarted = true;
                }
                else if (encoderStatus >= 0) // We have valid data
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

                    break;
                }
            }

            return timestamp;
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
