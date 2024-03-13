#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;

namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer
    {
        public GlTextureFrameBuffer(GL gl, GlTexture2D color, GlTexture2D? depth)
            : base(gl)
        {
     
            Color = color;
            Depth = depth;

            Create();
        }

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();

            BindDraw();

            _gl.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                Color.Target,
                Color, 0);

            if (Depth != null)
            {
                _gl.FramebufferTexture2D(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                    Depth.Target,
                    Depth, 0);
            }

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }

            Unbind();
        }

        public override void BindDraw()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _handle);
        }

        public void CopyTo(GlTextureFrameBuffer dest)
        {
            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _handle);
            /*
            _gl.FramebufferTexture2D(
                FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                Color.Target,
                Color, 0);
             */
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);


            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.Handle);
            /*
            _gl.FramebufferTexture2D(
                 FramebufferTarget.DrawFramebuffer,
                 FramebufferAttachment.ColorAttachment0,
                 dest.Color.Target,
                 dest.Color, 0);
             */
            _gl.DrawBuffers(1, DrawBufferMode.ColorAttachment0);

            _gl.BlitFramebuffer(0, 0, (int)Color.Width, (int)Color.Height, 0, 0, (int)dest.Color.Width, (int)dest.Color.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        }

        public override uint QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return Color.Handle;
            if (attachment == FramebufferAttachment.DepthAttachment)
                return Depth?.Handle ?? 0;

            throw new NotSupportedException();
        }


        public GlTexture2D Color;

        public GlTexture2D? Depth;

    }
}
