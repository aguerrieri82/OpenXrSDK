#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{


    public class GlMultiViewRenderTarget : IGlRenderTargetFB
    {
        protected GlMultiViewFrameBuffer _frameBuffer;

        protected readonly GL _gl;

        public GlMultiViewRenderTarget(GL gl)
        {
            _frameBuffer = new GlMultiViewFrameBuffer(gl);
            _gl = gl;
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
            if (discardDepth)
                _frameBuffer.Invalidate(InvalidateFramebufferAttachment.DepthStencilAttachment);

            _frameBuffer.Unbind();
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;

        public GlRenderTargetFlags Flags { get; set; }

        public int ShadingRate { get; set; }

        public Size2I RenderSize { get; set; }

        public Rect2I[]? ClipRegions { get; set; }
    }
}
