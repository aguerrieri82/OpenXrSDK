using Android.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Encoding = Android.Media.Encoding;

namespace XrEngine.Media.Android
{
    public class AndroidAudioDecoder : IAudioDecoder
    {

        static AudioFormat GetFormat(MediaFormat format)
        {

            var res = new AudioFormat
            {
                SampleRate = format.GetInteger(MediaFormat.KeySampleRate),

                Channels = format.GetInteger(MediaFormat.KeyChannelCount),

                SampleType = AudioSampleType.Short
            };

            if (format.ContainsKey(MediaFormat.KeyPcmEncoding))
            {
                res.SampleType = (Encoding)format.GetInteger(MediaFormat.KeyPcmEncoding) switch
                {
                    Encoding.Pcm8bit => AudioSampleType.Byte,
                    Encoding.Pcm16bit => AudioSampleType.Short,
                    Encoding.PcmFloat => AudioSampleType.Float,
                    _ => throw new NotSupportedException()
                };
            }

            return res;
        }

        public byte[] DecodeToPCM(string path, out AudioFormat format)
        {
            using var extractor = new MediaExtractor();
            extractor.SetDataSource(path);

            int trackIndex = -1;
            MediaFormat? inFormat = null;
            for (int i = 0; i < extractor.TrackCount; i++)
            {
                var f = extractor.GetTrackFormat(i);
                string mime = f.GetString(MediaFormat.KeyMime)!;
                if (mime.StartsWith("audio/"))
                {
                    trackIndex = i;
                    inFormat = f;
                    break;
                }
            }

            if (trackIndex < 0 || inFormat == null)
                throw new InvalidOperationException("No audio track found");

            extractor.SelectTrack(trackIndex);

            string mimeType = inFormat.GetString(MediaFormat.KeyMime)!;
            using var codec = MediaCodec.CreateDecoderByType(mimeType);
            codec.Configure(inFormat, null, null, 0);
            codec.Start();

            format = GetFormat(codec.OutputFormat);

            var info = new MediaCodec.BufferInfo();
            using var pcmStream = new MemoryStream(10 * 1024 * 1024);

            bool endOfStream = false;

            while (!endOfStream)
            {

                int inputIndex = codec.DequeueInputBuffer(10_000);
                if (inputIndex >= 0)
                {
                    var inputBuffer = codec.GetInputBuffer(inputIndex)!;
                    int sampleSize = extractor.ReadSampleData(inputBuffer, 0);

                    if (sampleSize < 0)
                    {
                        codec.QueueInputBuffer(inputIndex, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                        endOfStream = true;
                    }
                    else
                    {
                        long presentationTimeUs = extractor.SampleTime;
                        codec.QueueInputBuffer(inputIndex, 0, sampleSize, presentationTimeUs, 0);
                        extractor.Advance();
                    }
                }


                int outputIndex = codec.DequeueOutputBuffer(info, 10_000);
                if (outputIndex >= 0)
                {
                    var outputBuffer = codec.GetOutputBuffer(outputIndex);
                    if (outputBuffer != null && info.Size > 0)
                    {
                        var chunk = new byte[info.Size];
                        outputBuffer.Position(info.Offset);
                        outputBuffer.Get(chunk, 0, info.Size);
                        pcmStream.Write(chunk, 0, info.Size);
                    }

                    codec.ReleaseOutputBuffer(outputIndex, false);

                    if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                        break;
                }
                else if (outputIndex == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    format = GetFormat(codec.OutputFormat);
                }
            }

            codec.Stop();
            codec.Release();

            return pcmStream.ToArray();
        }
    }
}
