using OpenAl.Framework;

namespace XrEngine.Audio
{
    public interface IAudioStream
    {
        void Start();

        void Stop();

        int Fill(byte[] data, float timeSec);

        int PrefBufferSize { get; }

        int PrefBufferCount { get; }

        float Length { get; }

        AudioFormat Format { get; }

        bool IsStreaming { get; }
    }
}
