using Common.Interop;
using XrMath;

namespace XrEngine
{
    public enum RenderEngineDebug
    {
        None = 0,
        Sync = 1
    }

    public interface IRenderEngine : IDisposable
    {
        void Render(RenderContext ctx, Rect2I view, bool flush);

        void SetRenderTarget(Texture2D? texture);

        void Suspend();

        void Resume();

        T? Feature<T>() where T : class;

        Texture2D? GetDepth();

        Texture2D? GetShadowMap();

        IList<TextureData>? ReadTexture(Texture texture, TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null, IList<IMemoryBuffer<byte>>? buffers = null);

        void CopyTexture(Texture2D src, Texture2D dst);

        Texture2D AttachTexture(uint texId);

        void LoadTexture(Texture2D texture);

        void EnableDebug(RenderEngineDebug mode);

        IDispatcher Dispatcher { get; }
    }
}
