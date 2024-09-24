#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
#endif



namespace XrEngine.OpenGL
{
    public static class GlDepthUtils
    {
        static Dictionary<object, GlTexture> _depthTextures = [];
        static GlTextureFrameBuffer? _destFB;
        static GlTextureFrameBuffer? _srcFB;

        static GlTexture GetDepthTexture(GL gl, uint width, uint height, uint arraySize, bool mutable)
        {
            string key;
            if (width == 0 || height == 0)
                key = "mutable"; 
            else
                key = $"{width}x{height}x{arraySize}";

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
            if (_destFB == null)
            {
                _destFB = new GlTextureFrameBuffer(gl);
                _destFB.Target = FramebufferTarget.DrawFramebuffer;
                _destFB.SetDrawBuffers();
            }

            if (_srcFB == null)
            {
                _srcFB = new GlTextureFrameBuffer(gl);
                _srcFB.Target = FramebufferTarget.ReadFramebuffer;
                _srcFB.SetDrawBuffers();
            }

            var depth = GetDepthTexture(gl, src.Depth!.Width, src.Depth.Height, arraySize, false);
            
            _destFB.Bind();
            
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

                _destFB.Check();

                gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height,
                    0, 0, (int)depth.Width, (int)depth.Height,
                    ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            }

            src.Bind();

            return depth;
        }

        public static GlTexture GetDepthUsingFramebuffer(GL gl, IGlFrameBuffer src)
        {
            if (_destFB == null)
            {
                _destFB = new GlTextureFrameBuffer(gl);
                _destFB.Target = FramebufferTarget.DrawFramebuffer;
                _destFB.SetDrawBuffers();
            }

            var depth = GetDepthTexture(gl, src.Depth!.Width, src.Depth.Height, 1, false);

            _destFB.Configure(null, depth, 1);

            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, src.Handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, _destFB.Handle);

            gl.BlitFramebuffer(0, 0, (int)src.Depth!.Width, (int)src.Depth.Height, 
                                0, 0, (int)_destFB.Depth!.Width, (int)_destFB.Depth.Height,
                                ClearBufferMask.DepthBufferBit,  BlitFramebufferFilter.Nearest);


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
