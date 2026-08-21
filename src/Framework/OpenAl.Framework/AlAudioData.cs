namespace OpenAl.Framework
{
    public class AlAudioData
    {
        public AlAudioData(AlAudioFormat format, Span<byte> buffer)
        {
            Format = format;
            Buffer = buffer.ToArray();
        }

        public AlAudioFormat Format { get; set; }

        public byte[] Buffer { get; set; }
    }
}
