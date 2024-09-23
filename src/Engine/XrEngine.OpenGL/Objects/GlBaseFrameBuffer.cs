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
            Target = FramebufferTarget.Framebuffer;
        }

        public virtual void Bind()
        {
            GlState.Current!.BindFrameBuffer(Target, _handle);
        }

        public virtual void Unbind()
        {
            GlState.Current!.BindFrameBuffer(Target, 0);
        }

        public abstract GlTexture? QueryTexture(FramebufferAttachment attachment);

        public override void Dispose()
        {

            _gl.DeleteFramebuffer(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        public FramebufferTarget Target { get; set; }
    }
}
