using Common.Interop;
using XrMath;

namespace XrEngine
{
    public enum RenderEngineDebug
    {
        None = 0,
        Sync = 1
    }

    public struct RenderEngineFeatures
    {
        public RenderEngineFeatures()
        {
            GpuName = "";
        }

        public bool ClipCullDistance;
        public bool PrimitiveBoundingBox;
        public bool GeometryShader;
        public bool TessellationShader;
        public bool ShaderFramebufferFetch;
        public bool Multiview2;
        public bool ShaderFramebufferFetchRate;
        public bool ImageExternalEssl3;
        public bool BufferStorage;
        public bool ClearTexture;
        public bool MultisampledRenderToTexture;
        public bool DisjointTimerQuery;
        public bool ClipControl;
        public int MaxVertexSsboBlocks;
        public int MaxTextureUnits;
        public int MaxVertexAttribs;
        public Size2I MaxTextureSize;
        public int MaxFragmentSsboBlocks;
        public string GpuName;
        public bool IsNvidia;
        public bool IsAngle;
        public bool IsWindows;
        public bool IsAndroid;
        public bool IsGlEs;
        public int MaxVertexTextureUnits;
        public bool ScalarBlockLayout;
        public bool HasDualSourceBlend;
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

        RenderEngineFeatures Features { get; }
    }
}
