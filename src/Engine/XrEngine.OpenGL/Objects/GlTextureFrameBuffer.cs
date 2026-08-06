#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;

namespace XrEngine.OpenGL
{

    public class GlTextureFrameBuffer : GlBaseFrameBuffer
    {
        protected uint _sampleCount;
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

            BeginUpdate();

            if (color != null)
                Attach(color, FramebufferAttachment.ColorAttachment0, true, (int)colorIndex);

            if (depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;

                Attach(depth, attachment, false, (int)depthIndex);
            }

            EndUpdate();
        }

        public void Configure(GlTexture? color, IGlRenderAttachment? depth, uint sampleCount)
        {
            Configure(color, 0, depth, 0, sampleCount);
        }

        public void Configure(uint colorTex, uint depthTex, uint sampleCount)
        {
            var color = colorTex == 0 ? null : GlTexture.Attach(_gl, colorTex);
            var depth = depthTex == 0 ? null : GlTexture.Attach(_gl, depthTex);

            Configure(color, depth, sampleCount);
        }

        protected override void Complete()
        {
            SetDrawModes(_drawModes);
        }

        public override void Attach(IGlRenderAttachment obj, FramebufferAttachment slot, bool useDraw, int layer = 0)
        {
            Bind();

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

            _attachments[slot] = new GlAttachmentInfo
            {
                Attachment = obj,
            };

            if (useDraw)
                _drawModes.Add((DrawBufferMode)slot);

            _isDirty = true;
        }

        public override void Detach(FramebufferAttachment attachment)
        {
            Bind();

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

            Check();
        }

        public unsafe void ReadColor(TextureData data, ReadBufferMode mode = ReadBufferMode.ColorAttachment0)
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
            data.Content = MemoryBuffer.CreateOrResize(data.Content, Color.Width * Color.Height * pixelSize);

            BindRead(mode);

            using var pData = data.Content.MemoryLock();

            _gl.ReadPixels(0, 0, Color!.Width, Color.Height, pixelFormat, pixelType, pData);
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

        public override GlTexture? Color => _color;

        public override IGlRenderAttachment? Depth => _depth;

        public override uint SampleCount => _sampleCount;
    }
}
