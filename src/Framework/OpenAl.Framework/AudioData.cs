namespace OpenAl.Framework
{
    public class AudioData
    {
        public AudioData(AlAudioFormat format, byte[] buffer)
        {
            Format = format;
            Buffer = buffer;
        }

        public AlAudioFormat Format { get; set; }

        public byte[] Buffer { get; set; }
    }
}
