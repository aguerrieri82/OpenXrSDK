#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public static class GlDepthUtils
    {
        static readonly Dictionary<object, GlTexture> _depthTextures = [];
        static GlTextureFrameBuffer? _dstFB;
        static GlTextureFrameBuffer? _srcFB;

        static GlTexture GetDepthTexture(GL gl, uint width, uint height, uint arraySize, bool mutable, TextureFormat format = TextureFormat.Depth32Stencil8)
        {
            string key;
            if (width == 0 || height == 0)
                key = "mutable";
            else
                key = $"{width}x{height}x{arraySize}x{format}";

            if (!_depthTextures.TryGetValue(key, out var tex))
            {
                if (width == 0 || height == 0)
                {
                    int[] view = new int[4];
                    gl.GetInteger(GetPName.Viewport, view);
                    width = (uint)view[2];
                    height = (uint)view[3];
                }

                var data = new TextureData
                {
                    Width = width,
                    Height = height,
                    Format = format,
                };

                tex = new GlTexture(gl)
                {
                    MaxLevel = 0,
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    IsMutable = mutable,
                    BorderColor = new XrMath.Color(1, 1, 1, 1),
                    WrapS = TextureWrapMode.ClampToBorder,
                    WrapT = TextureWrapMode.ClampToBorder,
                    Target = arraySize > 1 ? TextureTarget.Texture2DArray : TextureTarget.Texture2D,
                };

                tex.Update(arraySize, data);

                _depthTextures[key] = tex;
            }

            return tex;
        }

        public static GlTexture GetDepthUsingFramebufferArray(GL gl, IGlFrameBuffer src, uint arraySize)
        {
            if (_dstFB == null)
            {
                _dstFB = new GlTextureFrameBuffer(gl);
                _dstFB.Target = FramebufferTarget.DrawFramebuffer;
                _dstFB.SetDrawBuffers();
            }

            if (_srcFB == null)
            {
                _srcFB = new GlTextureFrameBuffer(gl);
                _srcFB.Target = FramebufferTarget.ReadFramebuffer;
                _srcFB.SetDrawBuffers();
            }

            var depth = GetDepthTexture(gl, src.Depth!.Width, src.Depth.Height, arraySize, false);

            _dstFB.Bind();

            _srcFB.Bind();

            for (var i = 0; i < arraySize; i++)
            {
                gl.FramebufferTextureLayer(
                    FramebufferTarget.ReadFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                     src.Depth!.Handle, 0, i);

                _srcFB.Check();

                gl.FramebufferTextureLayer(
                    FramebufferTarget.DrawFramebuffer,
                    FramebufferAttachment.DepthAttachment,
                     depth.Handle, 0, i);

                _dstFB.Check();

                gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height,
                    0, 0, (int)depth.Width, (int)depth.Height,
                    ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            }

            src.Bind();

            return depth;
        }

        public static GlTexture GetDepthUsingFramebuffer(GL gl, IGlFrameBuffer src)
        {
            if (_dstFB == null)
            {
                _dstFB = new GlTextureFrameBuffer(gl);
                _dstFB.Target = FramebufferTarget.DrawFramebuffer;
                _dstFB.SetDrawBuffers();
            }

            var depth = GetDepthTexture(gl, src.Depth!.Width, src.Depth.Height, 1, false);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, src.Handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _dstFB.Handle);

            gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height,
                                0, 0, (int)depth.Width, (int)depth.Height,
                                ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);


            src.Bind();

            return depth;
        }

        public static GlTexture GetDepthUsingCopy(GL gl, object src)
        {
            var depth = GetDepthTexture(gl, 0, 0, 1, true);

            depth.Bind();

            gl.CopyTexImage2D(depth.Target, 0, InternalFormat.DepthComponent, 0, 0, depth.Width, depth.Height, 0);

            depth.Unbind();

            return depth;
        }

    }
}
