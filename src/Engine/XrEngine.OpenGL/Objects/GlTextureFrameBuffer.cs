#if GLES
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.EXT;
#else
using Silk.NET.OpenGL;
#endif

using System.Runtime.InteropServices;

namespace XrEngine.OpenGL
{
    public class GlTextureFrameBuffer : GlBaseFrameBuffer, IGlFrameBuffer
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void FramebufferTextureMultiviewOVRDelegate(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            uint texture,
            uint level,
            uint baseViewIndex,
            uint numViews);
        
        static FramebufferTextureMultiviewOVRDelegate? FramebufferTextureMultiviewOVR;


        private uint _sampleCount;

#if GLES
        ExtMultisampledRenderToTexture _extMs;  
#endif

        public GlTextureFrameBuffer(GL gl)
           : base(gl)
        {
#if GLES
            gl.TryGetExtension(out _extMs);
#endif

            Create();

            gl.Context.TryGetProcAddress("glFramebufferTextureMultiviewOVR", out var addr);
            FramebufferTextureMultiviewOVR = Marshal.GetDelegateForFunctionPointer<FramebufferTextureMultiviewOVRDelegate>(addr);

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

        public void Configure(GlTexture? color, IGlRenderAttachment? depth, uint sampleCount)
        {
            _sampleCount = sampleCount;

            Color = color;
            Depth = depth;


            Bind();

            if (Color != null)
            {

                bool useMs = false;
                if (sampleCount > 1)
                {
#if GLES
                    _extMs.FramebufferTexture2DMultisample(
                        Target,
                        FramebufferAttachment.ColorAttachment0,
                        Color.Target,
                        Color, 0, sampleCount);
                    useMs = true;   
#endif
                }

                if (!useMs)
                {
                    _gl.FramebufferTexture2D(
                        Target,
                        FramebufferAttachment.ColorAttachment0,
                        Color.Target,
                        Color, 0);
                }

            }

            if (Depth is GlTexture tex)
            {
                FramebufferAttachment attachment;

                if (tex.InternalFormat == InternalFormat.Depth24Stencil8 ||
                    tex.InternalFormat == InternalFormat.Depth24Stencil8Oes ||
                    tex.InternalFormat == InternalFormat.Depth32fStencil8 ||
                    tex.InternalFormat == InternalFormat.Rgba)
                    attachment = FramebufferAttachment.DepthStencilAttachment;
                else
                    attachment = FramebufferAttachment.DepthAttachment;

                bool useMs = false;
                if (sampleCount > 1)
                {
#if GLES
                    _extMs.FramebufferTexture2DMultisample(
                        Target,
                        attachment,
                        tex.Target,
                        tex, 0, sampleCount);
                    useMs = true;   
#endif
                }

                if (!useMs)
                {
                    if (tex.Depth > 1)
                        FramebufferTextureMultiviewOVR!(
                            Target,
                            attachment,
                            tex,
                            0, 0, tex.Depth);
                    else
                        _gl.FramebufferTexture2D(
                                Target,
                                attachment,
                                tex.Target,
                                tex, 0);
                }
            }

            else if (Depth is GlRenderBuffer rb)
            {

                _gl.FramebufferRenderbuffer(Target,
                         FramebufferAttachment.DepthStencilAttachment,
                         rb.Target,
                         rb.Handle);
            }

            Check();

            //Unbind();
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

            //Unbind();
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

        protected void Create()
        {
            _handle = _gl.GenFramebuffer();
        }

        public void CopyTo(GlTextureFrameBuffer dest, ClearBufferMask mask = ClearBufferMask.ColorBufferBit)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, dest.Handle);

            if (mask == ClearBufferMask.ColorBufferBit)
            {
                SetReadBuffer(ReadBufferMode.ColorAttachment0);
                dest.SetDrawBuffers(DrawBufferMode.ColorAttachment0);
            }

            var srcTex = mask == ClearBufferMask.ColorBufferBit ? Color : Depth;
            var dstTex = mask == ClearBufferMask.ColorBufferBit ? dest.Color : dest.Depth;
       
            _gl.BlitFramebuffer(0, 0, (int)srcTex!.Width, (int)srcTex.Height, 0, 0, (int)dstTex!.Width, (int)dstTex.Height, mask, BlitFramebufferFilter.Nearest);

            //GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, 0);
            //GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0);
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
