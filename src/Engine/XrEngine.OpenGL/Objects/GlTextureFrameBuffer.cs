#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer
    {
        public GlTextureFrameBuffer(GL gl)
           : base(gl)
        {
            Create();
        }

        public GlTextureFrameBuffer(GL gl, uint colorTex, uint depthTex, uint sampleCount = 1)
            : base(gl)
        {
            Configure(colorTex, depthTex, sampleCount);
        }


        public GlTextureFrameBuffer(GL gl, GlTexture color, GlTexture? depth)
            : base(gl)
        {
            Configure(color, depth);
        }

        public void Configure(GlTexture color, GlTexture? depth)
        {
            Color = color;
            Depth = depth;

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

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            var color = new GlTexture(_gl, colorTex, sampleCount);
            var depth = depthTex == 0 ? null : new GlTexture(_gl, depthTex, sampleCount);
            Configure(color, depth);
        }

        public unsafe TextureData Read()
        {
            if (Color!.InternalFormat != InternalFormat.Rgba8)
                throw new NotSupportedException();

            var data = new TextureData
            {
                Width = Color.Width,
                Height = Color.Height,
                Compression = TextureCompressionFormat.Uncompressed,
                Face = 0,
                MipLevel = 0,
                Depth = 0,
                Format = TextureFormat.Rgba32,
                Data = new Memory<byte>(new byte[Color.Width * Color.Height * 4])
            };

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _handle);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            fixed (byte* pData = data.Data.Span)
                _gl.ReadPixels(0, 0, Color.Width, Color.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pData);

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            return data;
        }

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();
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

            _gl.BlitFramebuffer(0, 0, (int)Color!.Width, (int)Color.Height, 0, 0, (int)dest.Color!.Width, (int)dest.Color.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        }

        public override uint QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.ColorAttachment0)
                return Color!.Handle;
            if (attachment == FramebufferAttachment.DepthAttachment)
                return Depth?.Handle ?? 0;

            throw new NotSupportedException();
        }


        public GlTexture? Color { get; set; }

        public GlTexture? Depth { get; set; }

    }
}
