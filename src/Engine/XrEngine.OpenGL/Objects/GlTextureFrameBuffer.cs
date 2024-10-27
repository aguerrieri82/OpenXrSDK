﻿#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
using System.Net.Mail;
using XrMath;
using static System.Net.Mime.MediaTypeNames;
#endif


namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer, IGlFrameBuffer
    {
        private uint _sampleCount;
        private Dictionary<FramebufferAttachment, IGlRenderAttachment> _attachments = [];

#if GLES
        readonly ExtMultisampledRenderToTexture _extMs;
#endif

        public GlTextureFrameBuffer(GL gl)
           : base(gl)
        {
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

        public void Configure(GlTexture? color, uint colorIndex, GlTexture? depth, uint depthIndex, uint sampleCount)
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
            }
            if (depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;

                _gl.FramebufferTextureLayer(
                    Target,
                    attachment,
                    depth,
                    0,
                    (int)depthIndex);
            }

            Check();
        }


        public void Configure(GlTexture? color, IGlRenderAttachment? depth, uint sampleCount)
        {
            _sampleCount = sampleCount;

            Color = color;
            Depth = depth;

            Bind();

            if (Color != null)
                BindAttachment(Color, FramebufferAttachment.ColorAttachment0);

            if (Depth != null)
            {
                var attachment = GlUtils.IsDepthStencil(Depth.InternalFormat) ?
                    FramebufferAttachment.DepthStencilAttachment :
                    FramebufferAttachment.DepthAttachment;
                BindAttachment(Depth, attachment);
            }

            Check();
        }

        protected void BindAttachment(IGlRenderAttachment obj, FramebufferAttachment slot)
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
                BindAttachment(glTex, slot);         
                SetDrawBuffers(DrawBufferMode.ColorAttachment0, (DrawBufferMode)slot);
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

            var dataSize = Color.Width * Color.Height * 4;

            if (data.Data.Length != dataSize)
                data.Data = new Memory<byte>(new byte[dataSize]);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);
            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            fixed (byte* pData = data.Data.Span)
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


        public GlTexture? Color { get; protected set; }

        public IGlRenderAttachment? Depth { get; protected set; }
    }
}
