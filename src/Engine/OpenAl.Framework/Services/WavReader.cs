namespace OpenAl.Framework
{
    public struct wav_header
    {
        // RIFF Header
        readonly int riff_header; // Contains "RIFF"
        readonly int wav_size; // Size of the wav portion of the file, which follows the first 8 bytes. File size - 8
        readonly int wave_header; // Contains "WAVE"

        // Format Header
        readonly int fmt_header; // Contains "fmt " (includes trailing space)
        readonly int fmt_chunk_size; // Should be 16 for PCM
        readonly short audio_format; // Should be 1 for PCM. 3 for IEEE Float
        readonly short num_channels;
        readonly int sample_rate;
        readonly int byte_rate; // Number of bytes per second. sample_rate * num_channels * Bytes Per Sample
        readonly short sample_alignment; // num_channels * Bytes Per Sample
        readonly short bit_depth; // Number of bits per sample

        // Data
        readonly char data_header; // Contains "data"
        readonly int data_bytes; // Number of bytes in data. Number of samples * num_channels * sample byte size
                                 // uint8_t bytes[]; // Remainder of wave file is bytes
    }

    public class WavReader
    {
        //static int RIFF = StrToInt();



        public AudioData Decode(Stream stream)
        {
            return null;
        }
    }
}
