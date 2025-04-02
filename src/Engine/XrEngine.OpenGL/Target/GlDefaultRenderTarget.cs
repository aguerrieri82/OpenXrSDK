#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
using SkiaSharp;

#endif

using XrMath;


namespace XrEngine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget, IGlFrameBufferProvider
    {
        readonly GL _gl;
        private readonly GlTexture _color;
        private readonly IGlRenderAttachment _depth;
        private readonly GlTextureFrameBuffer _frameBuffer;


        public GlDefaultRenderTarget(GL gl, bool useRenderBuffer)
        {
            _gl = gl;

            _color = new GlTexture(_gl)
            {
                MaxLevel = 0,
                IsMutable = true
            };

            if (useRenderBuffer)
                _depth = new GlRenderBuffer(_gl);
            else
                _depth = new GlTexture(_gl)
                {
                    IsMutable = true,
                    MaxLevel = 0,
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                };

            SetSize(new Size2I(16, 16));

            _frameBuffer = new GlTextureFrameBuffer(_gl, _color, _depth);
        }



        protected void SetSize(Size2I size)
        {
            var realSize = new Size2I((uint)Math.Ceiling(size.Width / 2.0f) * 2,
                                      (uint)Math.Ceiling(size.Height / 2.0f) * 2);

            var data = new TextureData
            {
                Width = realSize.Width,
                Height = realSize.Height,
                Format = TextureFormat.Rgba32
            };

            _color.Update(1, data);

            if (_depth is GlRenderBuffer renderBuffer)
                renderBuffer.Update(realSize.Width, realSize.Height, 1, InternalFormat.Depth24Stencil8);

            else if (_depth is GlTexture texture)
                texture.Update(1, new TextureData
                {
                    Width = realSize.Width,
                    Height = realSize.Height,
                    Format = TextureFormat.Depth24Stencil8,
                });
        }

        public void Begin(Camera camera)
        {
            GlState.Current!.SetView(new Rect2I(camera.ViewSize));

            if (camera.ViewSize.Width != _frameBuffer.Color!.Width || camera.ViewSize.Height != _frameBuffer.Color.Height)
                SetSize(camera.ViewSize);

            _frameBuffer.Bind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void End(bool discardDepth)
        {
            GlState.Current!.BindFrameBuffer(FramebufferTarget.ReadFramebuffer, _frameBuffer.Handle);
            GlState.Current!.BindFrameBuffer(FramebufferTarget.DrawFramebuffer, 0);

            _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GlState.Current.SetDrawBuffers(GlState.DRAW_BACK);

            var w = _frameBuffer.Color!.Width;
            var h = _frameBuffer.Color.Height;

            _gl.BlitFramebuffer(0, 0, (int)w, (int)h, 0, 0, (int)w, (int)h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            _frameBuffer.Unbind();
        }

        public void CommitDepth()
        {

        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            if (attachment == FramebufferAttachment.DepthAttachment)
                return _frameBuffer.QueryTexture(FramebufferAttachment.DepthAttachment);

            return null;
        }

        public IGlFrameBuffer FrameBuffer => _frameBuffer;

    }
}
