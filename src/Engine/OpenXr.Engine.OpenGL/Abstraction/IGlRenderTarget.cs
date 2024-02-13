using Silk.NET.OpenGLES;

namespace OpenXr.Engine.OpenGL
{
    public interface IGlRenderTarget : IDisposable
    {
        void Begin();

        void End();

        uint QueryTexture(FramebufferAttachment attachment);
    }
}
