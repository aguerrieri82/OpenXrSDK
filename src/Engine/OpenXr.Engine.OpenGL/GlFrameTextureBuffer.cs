using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public class GlFrameTextureBuffer : GlFrameBuffer
    {
        public GlFrameTextureBuffer(GL gl, GlTexture2D color)
            : base(gl)
        {
            Color = color;
            Depth = new GlTexture2D(_gl);
            Depth.Create(Color.Width, Color.Height, TextureFormat.Deph32Float);
        }

        public override void Bind()
        {
            base.Bind();    

            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                Color, 0);

            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D,
                Depth,
              0);
        }


        public GlTexture2D Color;

        public GlTexture2D Depth;

    }
}
