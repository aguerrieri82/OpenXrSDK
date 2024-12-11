namespace OpenAl.Framework
{
    public enum AudioSampleType
    {
        Byte,
        Short,
        Float
    }

    public class AudioFormat
    {
        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public AudioSampleType SampleType { get; set; }

        public int BitsPerSample => SampleType switch
        {
            AudioSampleType.Byte => 8,
            AudioSampleType.Short => 16,
            AudioSampleType.Float => 32,
            _ => 0
        };

    }
}
