#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget
    {
        readonly GL _gl;
        private Texture2D? _depthTexture;

        public GlDefaultRenderTarget(GL gl)
        {
            _gl = gl;
        }

        public void Begin()
        {
            GlState.Current!.BindFrameBuffer(0);
        }

        public void Dispose()
        {
        }

        public void End(bool finalPass)
        {
        }

        public void CommitDepth()
        {
  
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment)
            {
                if (_depthTexture == null)
                {
                    int[] view = new int[4];
                    _gl.GetInteger(GetPName.Viewport, view);

                    _depthTexture = new Texture2D();
                    _depthTexture.MaxLevels = 1;
                    _depthTexture.MinFilter = ScaleFilter.Nearest;
                    _depthTexture.MagFilter = ScaleFilter.Nearest;
                    _depthTexture.Format = TextureFormat.Depth24Float;
                    _depthTexture.IsMutable = true;
                    _depthTexture.BorderColor = new XrMath.Color(1, 1, 1, 1);
                    _depthTexture.WrapS = WrapMode.ClampToBorder;
                    _depthTexture.WrapT = WrapMode.ClampToBorder;
                    _depthTexture.Data = [new TextureData
                    {
                        Width =  (uint)view[2],
                        Height = (uint)view[3],
                        Format = TextureFormat.Depth24Float,
                    }];

                    _depthTexture.Width = _depthTexture.Data[0].Width;
                    _depthTexture.Height = _depthTexture.Data[0].Height;

                }

                var glTex = OpenGLRender.Current!.GetGlResource(_depthTexture);

                glTex.Bind();
                _gl.CopyTexImage2D(glTex.Target, 0, InternalFormat.DepthComponent, 0, 0, _depthTexture.Width, _depthTexture.Height, 0);
                glTex.Unbind();

                return glTex;
            }

            return null;
        }
    }
}
