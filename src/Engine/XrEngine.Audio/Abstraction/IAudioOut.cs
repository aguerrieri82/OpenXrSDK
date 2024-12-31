using OpenAl.Framework;

namespace XrEngine.Audio
{
    public interface IAudioOut
    {
        void Open(AudioFormat format);

        void Close();

        void Reset();

        void Enqueue(byte[] buffer);

        byte[]? Dequeue(int timeoutMs);
    }
}
