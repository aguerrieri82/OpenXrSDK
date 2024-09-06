
using XrMath;

namespace XrEngine.Video
{
    public interface IVideoReader : IDisposable
    {
        void Open(Uri source);

        bool TryDecodeNextFrame(TextureData data);

        void Close();

        double FrameRate { get; }

        Size2I FrameSize { get; }

        Texture2D? OutTexture { get; set; }
    }
}
