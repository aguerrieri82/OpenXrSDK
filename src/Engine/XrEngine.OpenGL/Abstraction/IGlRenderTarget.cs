#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public enum GlRenderTargetFlags
    {
        None = 0,
        Main = 0x1,

        ForceSrgbEncode = 0x2
    }

    public interface IGlRenderTarget : IDisposable
    {
        void Begin(Camera camera);

        void End(bool discardDepth);

        GlTexture? QueryTexture(FramebufferAttachment attachment);

        IShaderHandler? ShaderHandler => null;

        GlRenderTargetFlags Flags { get; }

        int ShadingRate { get; set; }

        Size2I RenderSize { get; set; }
    }

    public interface IGlRenderTargetFB : IGlRenderTarget, IGlFrameBufferProvider
    {

    }
}
