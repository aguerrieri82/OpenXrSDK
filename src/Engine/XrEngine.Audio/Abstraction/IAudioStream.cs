using OpenAl.Framework;

namespace XrEngine.Audio
{
    public interface IAudioStream
    {
        void Start();

        void Stop();

        int Fill(Span<byte> data, float timeSec);

        int PrefBufferSizeSamples { get; }

        int PrefBufferCount { get; }

        float Length { get; }

        AudioFormat Format { get; }

        bool IsStreaming { get; }
    }
}
