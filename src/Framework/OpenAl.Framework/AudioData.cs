namespace OpenAl.Framework
{
    public class AudioData
    {
        public AudioData(AudioFormat format, byte[] buffer)
        {
            Format = format;
            Buffer = buffer;
        }

        public AudioFormat Format { get; set; }

        public byte[] Buffer { get; set; }
    }
}
