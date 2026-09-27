using XrEngine.Media;

namespace XrEngine.Music
{
    public class WaveAudioBlock : BaseAudioBlock
    {
        public WaveAudioBlock(byte[] data, AudioFormat format)
        {
            Data = data;
            _duration = data.Length / (float)(format.SampleRate * format.Channels / (format.BitsPerSample / 8));
        }

        public byte[] Data { get; set; }
    }
}
