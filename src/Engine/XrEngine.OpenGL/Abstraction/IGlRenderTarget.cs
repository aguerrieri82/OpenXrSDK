#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using XrMath;

namespace XrEngine.OpenGL
{
    public interface IGlRenderTarget : IDisposable
    {
        void Begin(Camera camera, Size2I viewSize);

        void End(bool finalPass);

        GlTexture? QueryTexture(FramebufferAttachment attachment);

        void CommitDepth();
    }
}
