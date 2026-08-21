#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlTextureRenderTarget : IGlRenderTargetFB
    {
        protected readonly GlTextureFrameBuffer _frameBuffer;
        protected readonly GL _gl;

        public GlTextureRenderTarget(GL gl)
        {
            _gl = gl;
            _frameBuffer = new GlTextureFrameBuffer(_gl);
        }

        public void Begin(Camera camera)
        {
            if (RenderSize.Width == 0 || RenderSize.Height == 0)
                camera.ViewSize = _frameBuffer.Size;
            else
                camera.ViewSize = RenderSize;

            GlState.Current.SetView(new Rect2I(camera.ViewSize));

            _frameBuffer.BindDraw();

            OpenGLRender.Current!.Begin(this);
        }

        public void End(bool discardDepth)
        {
            if (discardDepth && _frameBuffer.Depth != null)
                _frameBuffer.Invalidate(InvalidateFramebufferAttachment.DepthAttachment);

            _frameBuffer.Unbind();
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();

            GC.SuppressFinalize(this);
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public GlTextureFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;

        public GlRenderTargetFlags Flags { get; set; }

        public int ShadingRate { get; set; }

        public Size2I RenderSize { get; set; }

        public Rect2I[]? ClipRegions { get; set; }
    }
}
