using System.Diagnostics;
using System.Text;

namespace OpenAl.Framework
{
    public struct wav_header
    {
        // RIFF Header
        public int riff_header; // Contains "RIFF"
        public int wav_size; // Size of the wav portion of the file, which follows the first 8 bytes. File size - 8
        public int wave_header; // Contains "WAVE"

        // Format Header
        public int fmt_header; // Contains "fmt " (includes trailing space)
        public int fmt_chunk_size; // Should be 16 for PCM
        public short audio_format; // Should be 1 for PCM. 3 for IEEE Float
        public short num_channels;
        public int sample_rate;
        public int byte_rate; // Number of bytes per second. sample_rate * num_channels * Bytes Per Sample
        public short sample_alignment; // num_channels * Bytes Per Sample
        public short bit_depth; // Number of bits per sample

        // Data
        public int data_header; // Contains "data"
        public int data_bytes; // Number of bytes in data. Number of samples * num_channels * sample byte size
                               // uint8_t bytes[]; // Remainder of wave file is bytes
    }

    public class WavReader
    {
        static unsafe int StrToInt(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);
            fixed (byte* pData = bytes)
                return *(int*)pData;
        }

        static readonly int RIFF = StrToInt("RIFF");
        static readonly int WAVE = StrToInt("WAVE");
        static readonly int FMT = StrToInt("fmt ");
        static readonly int DATA = StrToInt("data");

        public AudioData Decode(Stream stream)
        {
            var header = stream.ReadStruct<wav_header>();

            if (header.riff_header != RIFF || header.wave_header != WAVE)
                throw new InvalidOperationException();

            AudioSampleType sampleType;

            if (header.audio_format == 1)
                sampleType = header.bit_depth switch
                {
                    8 => AudioSampleType.Byte,
                    16 => AudioSampleType.Short,
                    _ => throw new NotSupportedException()
                };
            else if (header.audio_format == 3)
                sampleType = header.bit_depth switch
                {
                    32 => AudioSampleType.Float,
                    _ => throw new NotSupportedException()
                };
            else
                throw new NotSupportedException();

            var result = new AudioData(
                new AudioFormat
                {
                    SampleType = sampleType,
                    Channels = header.num_channels,
                    SampleRate = header.sample_rate
                },
                new byte[header.data_bytes]);

            var tot = stream.Read(result.Buffer);
            Debug.Assert(tot == header.data_bytes);

            return result;
        }
    }
}
