#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Threading.Tasks;
#endif



namespace XrEngine.OpenGL
{
    public static class GlDepthUtils
    {
        static Dictionary<object, GlTexture> _depthTextures = [];
        static GlTextureFrameBuffer? _readFrameBuffer;

        static GlTexture GetDepthTexture(GL gl, object key, bool mutable)
        {
            if (!_depthTextures.TryGetValue(key, out var tex))
            {
                int[] view = new int[4];
                gl.GetInteger(GetPName.Viewport, view);

                var data = new TextureData
                {
                    Width = (uint)view[2],
                    Height = (uint)view[3],
                    Format = TextureFormat.Depth32Stencil8,
                };

                tex = new GlTexture(gl)
                {
                    MaxLevel = 0,
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    IsMutable = mutable,
                    BorderColor = new XrMath.Color(1, 1, 1, 1),
                    WrapS = TextureWrapMode.ClampToBorder,
                    WrapT = TextureWrapMode.ClampToBorder
                };

                tex.Update(data);

                _depthTextures[key] = tex;  
            }

            return tex;
        }

        public static GlTexture GetDepthUsingFramebuffer(GL gl, GlTextureFrameBuffer src)
        {
            if (_readFrameBuffer == null)
            {
                _readFrameBuffer = new GlTextureFrameBuffer(gl);
                _readFrameBuffer.Target = FramebufferTarget.DrawFramebuffer;
            }

            var depth = GetDepthTexture(gl, src, true);

            _readFrameBuffer.Configure(null, depth, 1);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, src.Handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _readFrameBuffer.Handle);

            gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height, 
                                0, 0, (int)_readFrameBuffer.Depth!.Width, (int)_readFrameBuffer.Depth.Height,
                                ClearBufferMask.DepthBufferBit,  BlitFramebufferFilter.Nearest);


            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, src.Handle);

            return depth;
        }

        public static GlTexture GetDepthUsingCopy(GL gl, object src)
        {
            var depth = GetDepthTexture(gl, src, true);

            depth.Bind();
            
            gl.CopyTexImage2D(depth.Target, 0, InternalFormat.DepthComponent, 0, 0, depth.Width, depth.Height, 0);

            depth.Unbind();

            return depth;
        }

    }
}
