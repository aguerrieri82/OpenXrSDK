#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using XrMath;


namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer
    {
        protected uint _sampleCount;
        private Size2I _size;
        protected readonly Dictionary<FramebufferAttachment, IGlRenderAttachment> _attachments = [];
        protected readonly MutableArray<DrawBufferMode> _drawModes;
        protected GlTexture? _color;
        protected IGlRenderAttachment? _depth;


#if GLES
        static ExtMultisampledRenderToTexture? _extMs;
#endif

        public GlTextureFrameBuffer(GL gl)
           : base(gl)
        {
            _drawModes = new MutableArray<DrawBufferMode> { Sort = true };
#if GLES
            if (_extMs == null)
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
            _color = color;
            _depth = depth;

            BindDraw();

            if (color != null)
            {
                _gl.FramebufferTextureLayer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    color,
                    0,
                    (int)colorIndex);

                _drawModes.Add(DrawBufferMode.ColorAttachment0);

                _isDirty = true;
            }

            if (depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;

                if (depth is GlTexture depthTex && (depthTex.Depth > 1 && depthTex.Target == TextureTarget.Texture2DArray))
                {
#if GLES__
                    _extMs?.FramebufferTexture2DMultisample(
                        Target,
                        attachment,
                        TextureTarget.Texture2DArray,
                        depthTex, (int)depthIndex, _sampleCount);

#else
                    _gl.FramebufferTextureLayer(
                        FramebufferTarget.Framebuffer,
                        attachment,
                        depthTex,
                        0,
                        (int)depthIndex);
#endif

                    _isDirty = true;
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
            _color = color;
            _depth = depth;

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
            else
                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthStencilAttachment,
                    TextureTarget.Texture2D,
                    0,
                    0);

            Check();

            UpdateSize();
        }


        public override void BindAttachment(IGlRenderAttachment obj, FramebufferAttachment slot, bool useDraw, int layer = 0)
        {
            if (obj is GlTexture tex)
            {
                var useMs = false;
        
                if (_sampleCount > 1 && tex.Target == TextureTarget.Texture2D)
                {
#if GLES
                    _extMs?.FramebufferTexture2DMultisample(
                        FramebufferTarget.Framebuffer,
                        slot,
                        TextureTarget.Texture2D,
                        tex, layer, _sampleCount);

                    useMs = true;
#endif
                }


                if (!useMs)
                {
                    if (tex.Target == TextureTarget.Texture2D || tex.Target == TextureTarget.Texture2DMultisample)
                    {
                        _gl.FramebufferTexture2D(
                            FramebufferTarget.Framebuffer,
                            slot,
                            tex.Target,
                            tex, layer);
                    }
                    else
                    {
                        _gl.FramebufferTextureLayer(
                           FramebufferTarget.Framebuffer,
                           slot,
                           tex,
                           0, layer);
                    }


                }
            }
            else if (obj is GlRenderBuffer rb)
            {
                _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                         slot,
                         rb.Target,
                         rb.Handle);
            }

            _attachments[slot] = obj;

            if (useDraw)
                _drawModes.Add((DrawBufferMode)slot);

            _isDirty = true;
        }

        public override GlTexture GetOrCreateEffect(FramebufferAttachment slot)
        {
            if (Color == null)
                throw new NotSupportedException();

            if (!_attachments.TryGetValue(slot, out var obj))
            {
                var glTex = Color.Clone(false);
                glTex.MaxLevel = 0;

                BindDraw();
                BindAttachment(glTex, slot, true);
                Check();

                obj = glTex;
            }

            return (GlTexture)obj;
        }

        public void Detach(FramebufferAttachment attachment)
        {
            BindDraw();

            if (attachment == FramebufferAttachment.ColorAttachment0 || Depth is GlTexture)
            {
                var target = attachment == FramebufferAttachment.ColorAttachment0 ? Color!.Target : ((GlTexture)Depth!).Target;

                _gl.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        attachment,
                        target,
                        0, 0);
            }
            else
                throw new NotSupportedException();

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

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
            if (Color == null)
                throw new NotSupportedException();

            GlUtils.GetPixelFormat(data.Format, out var pixelFormat, out var pixelType);

            var pixelSize = data.Format.GetPixelSizeBit() / 8;

            data.Width = Color.Width;
            data.Height = Color.Height;
            data.Compression = TextureCompressionFormat.Uncompressed;
            data.Layer = 0;
            data.MipLevel = 0;
            data.Depth = 0;
            data.Data = MemoryBuffer.CreateOrResize(data.Data, Color.Width * Color.Height * pixelSize);

            BindRead(ReadBufferMode.ColorAttachment0);

            using var pData = data.Data.MemoryLock();

            _gl.ReadPixels(0, 0, Color!.Width, Color.Height, pixelFormat, pixelType, pData);

            Unbind();
        }

        public TextureData ReadColor(TextureFormat format)
        {
            var data = new TextureData
            {
                Format = format
            };

            ReadColor(data);

            return data;
        }

        public void Invalidate(params InvalidateFramebufferAttachment[] attachments)
        {
            if (attachments.Length == 1 && 
                attachments[0] == InvalidateFramebufferAttachment.DepthAttachment &&
                Depth == null)
            {
                return;
            }

            _gl.InvalidateFramebuffer(FramebufferTarget.Framebuffer, attachments.AsSpan());
        }

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();
        }


        public override GlTexture? QueryTexture(FramebufferAttachment attachment)
        {

            if (attachment == FramebufferAttachment.ColorAttachment0)
                return Color;

            if (attachment == FramebufferAttachment.DepthAttachment)
            {
                if (Depth is GlRenderBuffer)
                    return GlImageProc.GetDepth(_gl, this);

                return Depth as GlTexture;
            }

            throw new NotSupportedException();
        }

        protected void UpdateSize()
        {
            if (Color != null)
                _size = new Size2I(Color.Width, Color.Height);
            else if (Depth != null)
                _size = new Size2I(Depth.Width, Depth.Height);
        }

        public override Size2I Size => _size;

        public override GlTexture? Color => _color;

        public override IGlRenderAttachment? Depth => _depth;

        public override uint SampleCount => _sampleCount;
    }
}
