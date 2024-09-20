#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public abstract class GlBaseFrameBuffer : GlObject
    {
        public GlBaseFrameBuffer(GL gl)
            : base(gl)
        {
        }

        public virtual void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
        }

        public virtual void Unbind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public abstract uint QueryTexture(FramebufferAttachment attachment);

        public override void Dispose()
        {

            _gl.DeleteFramebuffer(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }
    }
}
