namespace OpenAl.Framework
{
    public enum AlAudioSampleType
    {
        Byte,
        Short,
        Float
    }

    public class AlAudioFormat
    {
        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public AlAudioSampleType SampleType { get; set; }

        public int BitsPerSample => SampleType switch
        {
            AlAudioSampleType.Byte => 8,
            AlAudioSampleType.Short => 16,
            AlAudioSampleType.Float => 32,
            _ => 0
        };

    }
}
