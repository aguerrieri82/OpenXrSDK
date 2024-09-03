
using XrMath;

namespace XrEngine
{
    public interface IVideoDecoder : IDisposable
    {
        void Open(string filename);

        bool TryDecodeNextFrame(TextureData data);

        double FrameRate { get; }

        Size2I FrameSize { get; }

        Texture2D? OutTexture { get; set; }
    }
}
