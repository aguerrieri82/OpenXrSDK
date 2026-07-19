#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Drawing;

#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public struct GlAttachmentInfo
    {
        public IGlRenderAttachment Attachment;

        public uint Layer;
    }


    public abstract class GlBaseFrameBuffer : GlObject, IGlFrameBuffer
    {
        protected bool _isDirty = true;
        protected DrawBufferMode[] _lastDrawModes = [];
        protected ReadBufferMode _lastReadMode;
        protected int _updateCount;

        protected Size2I _size;

        protected readonly Dictionary<FramebufferAttachment, GlAttachmentInfo> _attachments = [];

        public GlBaseFrameBuffer(GL gl)
            : base(gl)
        {

        }

        public void Check(bool force = false)
        {
            if ((!_isDirty || _updateCount > 0) && !force)
                return;

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                Log.Warn(this, "Frame buffer state invalid: {0}", status);
               // throw new Exception($"Frame buffer state invalid: {status}");
            }

            Complete();

            _isDirty = false;
        }

        protected virtual void Complete()
        {

        }

        public void BeginUpdate()
        {
            if (_updateCount == 0)
                Bind();
            _updateCount++;
        }

        public void EndUpdate()
        {
            _updateCount--;

            if (_updateCount == 0)
            {
                Check();
                UpdateSize();
                Unbind();
            }
        }


        public virtual void Bind()
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.Framebuffer, _handle);
        }

        public void BindRead(ReadBufferMode mode)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);

            if (_lastReadMode == mode)
                return;

            _gl.ReadBuffer(mode);

            _lastReadMode = mode;
        }

        public virtual void BindDraw()
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _handle);
        }

        public virtual void BindDraw(params DrawBufferMode[] modes)
        {
            BindDraw();
            SetDrawModes(modes);
        }

        protected void SetDrawModes(DrawBufferMode[] modes)
        {
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


        public void Invalidate(params InvalidateFramebufferAttachment[] attachments)
        {
            if (attachments.Length == 1 &&
                attachments[0] == InvalidateFramebufferAttachment.DepthAttachment &&
                Depth == null)
            {
                return;
            }

            Bind();

            _gl.InvalidateFramebuffer(FramebufferTarget.Framebuffer, attachments.AsSpan());
        }

        public virtual GlTexture GetOrCreateEffect(FramebufferAttachment slot)
        {
            if (Color == null)
                throw new NotSupportedException();

            if (!_attachments.TryGetValue(slot, out var obj))
            {
                var glTex = Color.Clone(false);

                glTex.MaxLevel = 0;

                Attach(glTex, slot, true);

                Check();

                return glTex;
            }

            return (GlTexture)obj.Attachment;
        }


        public override void Dispose()
        {
            if (_handle != 0)
                _gl.DeleteFramebuffer(_handle);

            base.Dispose();
        }

        protected void UpdateSize()
        {
            if (Color != null)
                _size = new Size2I(Color.Width, Color.Height);
            else if (Depth != null)
                _size = new Size2I(Depth.Width, Depth.Height);
        }


        public abstract GlTexture? QueryTexture(FramebufferAttachment attachment);

        public abstract void Detach(FramebufferAttachment slot);

        public abstract void Attach(IGlRenderAttachment attachment, FramebufferAttachment slot, bool useDraw, int layer = 0);

        public Size2I Size => _size;

        public abstract GlTexture? Color { get; }

        public abstract IGlRenderAttachment? Depth { get; }

        public abstract uint SampleCount { get; }
    }
}
