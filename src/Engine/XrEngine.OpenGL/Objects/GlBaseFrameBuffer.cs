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

        public void Check()
        {
            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public void SetDrawBuffers(params DrawBufferMode[] modes)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _handle);

            if (modes.Length == 0)
                GlState.Current.SetDrawBuffers(GlState.DRAW_NONE);
            else
                GlState.Current.SetDrawBuffers(modes);
        }

        public void SetReadBuffer(ReadBufferMode mode)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);

            _gl.ReadBuffer(mode);
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
            if (_handle != 0)
            {
                _gl.DeleteFramebuffer(_handle);
                _handle = 0;
            }
            GC.SuppressFinalize(this);
        }

        public FramebufferTarget Target { get; set; }
    }
}
