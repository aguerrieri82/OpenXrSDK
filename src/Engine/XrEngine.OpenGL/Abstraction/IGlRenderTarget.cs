#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public enum GlRenderTargetFlags
    {
        None = 0,
        Main = 1
    }

    public interface IGlRenderTarget : IDisposable
    {
        void Begin(Camera camera);

        void End(bool discardDepth);

        GlTexture? QueryTexture(FramebufferAttachment attachment);

        void CommitDepth();

        IShaderHandler? ShaderHandler => null;


        GlRenderTargetFlags Flags { get; }

    }
}
