#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget
    {
        readonly GL _gl;

        public GlDefaultRenderTarget(GL gl)
        {
            _gl = gl;
        }

        public void Begin(Camera camera)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void End(bool finalPass)
        {
        }

        public void CommitDepth()
        {

        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment)
                return GlDepthUtils.GetDepthUsingCopy(_gl, this);

            return null;
        }
    }
}
