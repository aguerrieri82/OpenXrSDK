#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer, IGlFrameBuffer
    {
        protected uint _sampleCount;
        private Size2I _size;
        protected readonly Dictionary<FramebufferAttachment, IGlRenderAttachment> _attachments = [];
        protected readonly MutableArray<DrawBufferMode> _drawBuffers;

#if GLES
        readonly ExtMultisampledRenderToTexture _extMs;
#endif

        public GlTextureFrameBuffer(GL gl)
           : base(gl)
        {
            _drawBuffers = new MutableArray<DrawBufferMode> { Sort = true };    
#if GLES
            gl.TryGetExtension(out _extMs);
#endif
            Create();
        }

        public GlTextureFrameBuffer(GL gl, uint colorTex, uint depthTex, uint sampleCount = 1)
            : this(gl)
        {
            Configure(colorTex, depthTex, sampleCount);
        }


        public GlTextureFrameBuffer(GL gl, GlTexture? color, IGlRenderAttachment? depth, uint sampleCount = 1)
            : this(gl)
        {
            Configure(color, depth, sampleCount);
        }

        public void Configure(GlTexture? color, uint colorIndex, IGlRenderAttachment? depth, uint depthIndex, uint sampleCount)
        {
            _sampleCount = sampleCount;

            Color = color;
            Depth = depth;

            Bind();

            if (color != null)
            {
                _gl.FramebufferTextureLayer(
                    Target,
                    FramebufferAttachment.ColorAttachment0,
                    color,
                    0,
                    (int)colorIndex);

                _drawBuffers.Add(DrawBufferMode.ColorAttachment0);
            }

            if (depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;

                if (depth is GlTexture depthTex && (depthTex.Depth > 1 || depthTex.Target == TextureTarget.Texture2DArray))
                {
                    _gl.FramebufferTextureLayer(
                        Target,
                        attachment,
                        depthTex,
                        0,
                        (int)depthIndex);
                }
                else
                    BindAttachment(depth, attachment, false);
            }

            Check();

            UpdateSize();
        }


        public void Configure(GlTexture? color, IGlRenderAttachment? depth, uint sampleCount)
        {
            _sampleCount = sampleCount;

            Color = color;
            Depth = depth;

            Bind();

            if (Color != null)
                BindAttachment(Color, FramebufferAttachment.ColorAttachment0, true);

            if (Depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(Depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;
                BindAttachment(Depth, attachment, false);
            }

            Check();

            UpdateSize();
        }


        public override void Bind()
        {
            base.Bind();
            GlState.Current!.SetDrawBuffers(_drawBuffers.Data);
        }

        public void BindAttachment(IGlRenderAttachment obj, FramebufferAttachment slot, bool useDraw)
        {
            if (obj is GlTexture tex)
            {
                bool useMs = false;
                if (_sampleCount > 1)
                {
#if GLES
                    _extMs.FramebufferTexture2DMultisample(
                        Target,
                        slot,
                        tex.Target,
                        tex, 0, _sampleCount);
                    useMs = true;
#endif
                }

                if (!useMs)
                {
                    _gl.FramebufferTexture2D(
                        Target,
                        slot,
                        tex.Target,
                        tex, 0);

                }
            }
            else if (obj is GlRenderBuffer rb)
            {
                _gl.FramebufferRenderbuffer(Target,
                         slot,
                         rb.Target,
                         rb.Handle);
            }

            _attachments[slot] = obj;

            if (useDraw)
                _drawBuffers.Add((DrawBufferMode)slot);
        }

        public GlTexture GetOrCreateEffect(FramebufferAttachment slot)
        {
            if (Color == null)
                throw new NotSupportedException();

            if (!_attachments.TryGetValue(slot, out var obj))
            {
                var glTex = Color.Clone(false);
                glTex.MaxLevel = 0;

                Bind();
                BindAttachment(glTex, slot, true);
                Check();
                
                obj = glTex;
            }

            return (GlTexture)obj;
        }

        public void Detach(FramebufferAttachment attachment)
        {
            Bind();

            if (attachment == FramebufferAttachment.ColorAttachment0 || Depth is GlTexture)
            {
                var target = attachment == FramebufferAttachment.ColorAttachment0 ? Color!.Target : ((GlTexture)Depth!).Target;

                _gl.FramebufferTexture2D(
                        Target,
                        attachment,
                        target,
                        0, 0);
            }
            else
                throw new NotSupportedException();

            var status = _gl.CheckFramebufferStatus(Target);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            var color = colorTex == 0 ? null : GlTexture.Attach(_gl, colorTex);
            var depth = depthTex == 0 ? null : GlTexture.Attach(_gl, depthTex);

            Configure(color, depth, sampleCount);
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
            data.Data = MemoryBuffer.CreateOrResize(data.Data, Color.Width * Color.Height * 4);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            using var pData = data.Data.MemoryLock();

            _gl.ReadPixels(0, 0, Color!.Width, Color.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pData);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        public unsafe TextureData ReadColor()
        {
            var data = new TextureData();

            ReadColor(data);

            return data;
        }

        public void Invalidate(params InvalidateFramebufferAttachment[] attachments)
        {
            _gl.InvalidateFramebuffer(Target, attachments.AsSpan());
        }

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();
        }

        public void CopyTo(GlTextureFrameBuffer dst, ClearBufferMask mask = ClearBufferMask.ColorBufferBit)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, dst.Handle);

            if (mask == ClearBufferMask.ColorBufferBit)
            {
                SetReadBuffer(ReadBufferMode.ColorAttachment0);
                dst.SetDrawBuffers(DrawBufferMode.ColorAttachment0);
            }

            var srcTex = mask == ClearBufferMask.ColorBufferBit ? Color : Depth;
            var dstTex = mask == ClearBufferMask.ColorBufferBit ? dst.Color : dst.Depth;

            _gl.BlitFramebuffer(0, 0, (int)srcTex!.Width, (int)srcTex.Height, 0, 0, (int)dstTex!.Width, (int)dstTex.Height, mask, BlitFramebufferFilter.Nearest);
        }

        public override GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment && Depth is GlRenderBuffer)
                return GlDepthUtils.GetDepthUsingFramebuffer(_gl, this);

            if (attachment == FramebufferAttachment.ColorAttachment0)
                return Color;

            if (attachment == FramebufferAttachment.DepthAttachment)
                return Depth as GlTexture;

            throw new NotSupportedException();
        }

        protected void UpdateSize()
        {
            if (Color != null)
                _size = new Size2I(Color.Width, Color.Height);
            else if (Depth != null)
                _size = new Size2I(Depth.Width, Depth.Height);
        }

        public Size2I Size => _size;

        public GlTexture? Color { get; protected set; }

        public IGlRenderAttachment? Depth { get; protected set; }
    }
}
