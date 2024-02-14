#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace OpenXr.Engine.OpenGL
{
    public interface IGlRenderTarget : IDisposable
    {
        void Begin();

        void End();

        uint QueryTexture(FramebufferAttachment attachment);
    }
}
