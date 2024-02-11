#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics.CodeAnalysis;

namespace OpenXr.Engine.OpenGL
{
    public class GlFrameTextureBuffer : GlFrameBuffer
    {

        private uint _sampleCount;

        public GlFrameTextureBuffer(GL gl, GlTexture2D color, uint sampleCount = 1)
            : base(gl)
        {
            _handle = _gl.GenFramebuffer();
            _sampleCount = sampleCount;

            Color = color;

            if (sampleCount > 1) 
            {
                color.SampleCount = _sampleCount;

                color.Bind();

                _gl.TexStorage2DMultisample(
                     TextureTarget.Texture2DMultisample,
                     sampleCount,
                     SizedInternalFormat.Rgba16f,
                     color.Width,
                     color.Height,
                     true);

                color.Unbind();

            }

            CreateDepth();

            
        }

        [MemberNotNull(nameof(Depth))]
        protected unsafe void CreateDepth()
        {
            Depth = new GlTexture2D(_gl);
            Depth.MagFilter = TextureMagFilter.Nearest;
            Depth.MinFilter = TextureMinFilter.Nearest;
            Depth.WrapT = Color.WrapT;
            Depth.WrapS = Color.WrapS;
            Depth.MaxLevel = Color.MaxLevel;
            Depth.BaseLevel = Color.BaseLevel;
            Depth.SampleCount = _sampleCount;
            Depth.Create(Color.Width, Color.Height, TextureFormat.Depth24Float);
        }

        public override void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _handle);

            _gl.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                Color.Target,
                Color, 0);

            _gl.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.DepthAttachment,
                Depth.Target,
                Depth, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public void InvalidateDepth()
        {
            _gl.InvalidateFramebuffer(FramebufferTarget.DrawFramebuffer, [InvalidateFramebufferAttachment.DepthAttachment]);
        }

        public GlTexture2D Color;

        public GlTexture2D Depth;

    }
}
