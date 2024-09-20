#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public abstract class GlBaseFrameBuffer : GlObject
    {
        protected static readonly DrawBufferMode[] DRAW_COLOR_0 = [DrawBufferMode.ColorAttachment0];
        protected static readonly DrawBufferMode[] DRAW_NONE = [DrawBufferMode.None];

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
