
using XrEngine.Media;

namespace XrEngine.Devices
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
