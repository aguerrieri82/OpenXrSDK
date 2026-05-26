
using XrMath;

namespace XrEngine.Media
{
    public interface IVideoReader : IDisposable
    {
        void Open(Uri source, TextureFormat outFormat = TextureFormat.Rgba32);

        bool TryDecodeNextFrame(TextureData data);

        bool SeekToFrame(int targetFrameIndex);

        void Close();

        double FrameRate { get; }

        Size2I FrameSize { get; }

        Texture2D? OutTexture { get; set; }
    }
}
