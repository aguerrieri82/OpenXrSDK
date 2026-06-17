#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.Core.Native;
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
                    var view = new int[4];

                    gl.GetInteger(GetPName.Viewport, view);

                    width = (uint)view[2];
                    height = (uint)view[3];
                }

                var data = new TextureData
                {
                    Width = width,
                    Height = height,
                    Depth = arraySize,
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

                tex.Update(data);

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
            }
            
            if (_srcFB == null)
            {
                _srcFB = new GlTextureFrameBuffer(gl);
                _srcFB.Target = FramebufferTarget.ReadFramebuffer;
            }

            var depth = GetDepthTexture(gl, src.Depth!.Width, src.Depth.Height, arraySize, false, GlUtils.GetTextureFormat(src.Depth.InternalFormat));

            src.Unbind();

            _dstFB.Bind();

            _srcFB.Bind();

            var attachment = GlUtils.IsDepthStencil(depth.InternalFormat) ?
                FramebufferAttachment.DepthStencilAttachment :
                FramebufferAttachment.DepthAttachment;

            for (var i = 0; i < arraySize; i++)
            {
                if (src is GlMultiViewFrameBuffer)
                {
                    GlMultiViewFrameBuffer.FramebufferTextureMultisampleMultiviewOVR!(
                       FramebufferTarget.ReadFramebuffer,
                       attachment,
                       src.Depth.Handle,
                       0,
                       src.SampleCount,
                       (uint)i, 1);
                }
                else
                {
                    gl.FramebufferTextureLayer(
                        FramebufferTarget.ReadFramebuffer,
                        attachment,
                        src.Depth.Handle, 0, i);
                }

                gl.FramebufferTextureLayer(
                    FramebufferTarget.DrawFramebuffer,
                    attachment,
                    depth.Handle, 0, i);

                gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height,
                    0, 0, (int)depth.Width, (int)depth.Height,
                    ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            }

            _dstFB.Unbind();
            _srcFB.Unbind();

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

        public static GlTexture GetDepthUsingCopy(GL gl, IGlFrameBuffer src, GlTexture? depthTex)
        {
            var arraySize = (depthTex?.Depth == null ? 1u : depthTex.Depth);
            
            var mutable = arraySize == 1;

            var format = depthTex != null ? GlUtils.GetTextureFormat(depthTex.InternalFormat) : TextureFormat.Depth32Stencil8;
            var width = depthTex?.Width ?? 0;
            var height = depthTex?.Height ?? 0;

            var depthCopyTex = GetDepthTexture(gl, width, height, arraySize, mutable, format);

            if (depthTex != null)
            {
                depthTex.CopyTo(depthCopyTex);
            }
            else
            {
                depthCopyTex.Bind();

                GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, src.Handle);
                
                gl.CopyTexImage2D(depthCopyTex.Target, 0, InternalFormat.DepthComponent, 0, 0, depthCopyTex.Width, depthCopyTex.Height, 0);

                depthCopyTex.Unbind();
            }

            return depthCopyTex;
        }

        public static void Dispose()
        {
            foreach (var item in _depthTextures)
                item.Value.Dispose();

            _depthTextures.Clear();
        }

    }
}
