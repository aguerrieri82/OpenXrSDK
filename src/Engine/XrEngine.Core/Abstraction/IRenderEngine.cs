using XrMath;

namespace XrEngine
{
    public interface IRenderEngine : IDisposable
    {
        void Render(RenderContext ctx, Rect2I view, bool flush);

        void SetRenderTarget(Texture2D? texture);

        void Suspend();

        void Resume();

        Texture2D? GetDepth();

        Texture2D? GetShadowMap();

        IList<TextureData>? ReadTexture(Texture texture, TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null);

        IDispatcher Dispatcher { get; }
    }
}
