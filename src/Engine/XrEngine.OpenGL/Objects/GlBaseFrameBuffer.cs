#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseFrameBuffer : GlObject, IGlFrameBuffer
    {
        protected bool _isDirty = true;
        protected DrawBufferMode[] _lastDrawModes = [];
        protected ReadBufferMode _lastReadMode;

        public GlBaseFrameBuffer(GL gl)
            : base(gl)
        {

        }

        public void Check(bool force = false)
        {
            if (!_isDirty && !force)
                return;

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                //throw new Exception($"Frame buffer state invalid: {status}");
            }

            _isDirty = false;
        }


        public void BindRead(ReadBufferMode mode)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);

            if (_lastReadMode == mode)
                return;

            _gl.ReadBuffer(mode);

            _lastReadMode = mode;
        }


        public virtual void Bind()
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.Framebuffer, _handle);
        }

        public virtual void BindDraw()
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _handle);
        }

        public virtual void BindDraw(params DrawBufferMode[] modes)
        {
            BindDraw();

            if (Utils.ArrayEquals(modes, _lastDrawModes))
                return;

            if (modes.Length == 0)
                _gl.DrawBuffers(GlState.DRAW_NONE);
            else
                _gl.DrawBuffers(modes);

            _lastDrawModes = modes;
        }

        public virtual void Unbind()
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CopyTo(IGlFrameBuffer dst, ClearBufferMask mask = ClearBufferMask.ColorBufferBit)
        {
            if (mask != ClearBufferMask.ColorBufferBit)
                throw new NotSupportedException();

            BindRead(ReadBufferMode.ColorAttachment0);

            dst.BindDraw(DrawBufferMode.ColorAttachment0);

            var srcTex = mask == ClearBufferMask.ColorBufferBit ? Color : Depth;
            var dstTex = mask == ClearBufferMask.ColorBufferBit ? dst.Color : dst.Depth;

            _gl.BlitFramebuffer(0, 0, (int)srcTex!.Width, (int)srcTex.Height, 0, 0, (int)dstTex!.Width, (int)dstTex.Height, mask, BlitFramebufferFilter.Nearest);
        }


        public abstract GlTexture? QueryTexture(FramebufferAttachment attachment);

        public override void Dispose()
        {
            if (_handle != 0)
                _gl.DeleteFramebuffer(_handle);

            base.Dispose();
        }

        public abstract void BindAttachment(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw, int layer = 0);

        public abstract GlTexture GetOrCreateEffect(FramebufferAttachment slot);

        public abstract GlTexture? Color { get; }

        public abstract IGlRenderAttachment? Depth { get; }

        public abstract Size2I Size { get; }
        
        public abstract uint SampleCount { get; }
    }
}
