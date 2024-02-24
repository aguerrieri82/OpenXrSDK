#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace Xr.Engine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget
    {
        readonly GL _gl;

        public GlDefaultRenderTarget(GL gl)
        {
            _gl = gl;
        }

        public void Begin()
        {

        }

        public void Dispose()
        {
        }

        public void End()
        {
        }

        public uint QueryTexture(FramebufferAttachment attachment)
        {
            return 0;
        }
    }
}
