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
            : this(gl)
        {
            Configure(colorTex, depthTex, sampleCount);
        }


        public GlTextureFrameBuffer(GL gl, GlTexture? color, GlTexture? depth)
            : this(gl)
        {
            Configure(color, depth);
        }

        public void Configure(GlTexture? color, GlTexture? depth)
        {
            Color = color;
            Depth = depth;

            Bind();

            if (Color != null)
            {
                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    Color.Target,
                    Color, 0);
            }

            if (Depth != null)
            {
                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    Depth.Target,
                    Depth, 0);
            }

            if (Color == null)
            {
                _gl.DrawBuffers(DRAW_NONE);
                _gl.ReadBuffer(ReadBufferMode.None);    
            }
            else
            {
                _gl.DrawBuffers(DRAW_COLOR_0);
                _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            }

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }

            Unbind();
        }

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            var target = sampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D;

            var color = colorTex == 0 ? null : GlTexture.Attach(_gl, colorTex, sampleCount, target);
            var depth = depthTex == 0 ? null : GlTexture.Attach(_gl, depthTex, sampleCount, target);
            
            Configure(color, depth);
        }

        public unsafe void ReadColor(TextureData data)
        {
            if (Color == null || Color.InternalFormat != InternalFormat.Rgba8)
                throw new NotSupportedException();

            data.Width = Color.Width;
            data.Height = Color.Height;
            data.Compression = TextureCompressionFormat.Uncompressed;
            data.Face = 0;
            data.MipLevel = 0;
            data.Depth = 0;
            data.Format = TextureFormat.Rgba32;

            var dataSize = Color.Width * Color.Height * 4;

            if (data.Data.Length != dataSize)
                data.Data = new Memory<byte>(new byte[dataSize]);

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _handle);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            fixed (byte* pData = data.Data.Span)
                _gl.ReadPixels(0, 0, Color!.Width, Color.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pData);

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        public unsafe TextureData ReadColor()
        {
            var data = new TextureData();

            ReadColor(data);

            return data;
        }

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();
        }

        public void CopyTo(GlTextureFrameBuffer dest)
        {
            DrawBufferMode[] drawBuffers = [DrawBufferMode.ColorAttachment0];

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _handle);

            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.Handle);

            _gl.DrawBuffers(drawBuffers);

            _gl.BlitFramebuffer(0, 0, (int)Color!.Width, (int)Color.Height, 0, 0, (int)dest.Color!.Width, (int)dest.Color.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
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
