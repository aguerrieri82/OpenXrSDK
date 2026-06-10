#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public abstract class GlBaseFrameBuffer : GlObject
    {
        protected bool _isDirty = true;
        protected DrawBufferMode[] _lastDrawModes = [];
        protected ReadBufferMode _lastReadMode;

        public GlBaseFrameBuffer(GL gl)
            : base(gl)
        {
            Target = FramebufferTarget.Framebuffer;
        }

        public void Check(bool force = false)
        {
            if (!_isDirty && !force)
                return;

            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                //throw new Exception($"Frame buffer state invalid: {status}");
            }

            _isDirty = false;
        }

        public void SetDrawBuffers(params DrawBufferMode[] modes)
        {
            if (Utils.ArrayEquals(modes, _lastDrawModes))
                return;

            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _handle);

            if (modes.Length == 0)
                _gl.DrawBuffers(GlState.DRAW_NONE);
            else
                _gl.DrawBuffers(modes);

            _lastDrawModes = modes; 
        }

        public void SetReadBuffer(ReadBufferMode mode)
        {
            if (_lastReadMode == mode)
                return;

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);

            _gl.ReadBuffer(mode);

            _lastReadMode = mode;
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
                _gl.DeleteFramebuffer(_handle);

            base.Dispose();
        }

        public FramebufferTarget Target { get; set; }
    }
}
