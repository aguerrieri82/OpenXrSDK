#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;


namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProviderV2 : IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;

        public GlMotionVectorProviderV2(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;

            if (XrPlatform.IsEditor)
                MotionVectorFormat = (long)InternalFormat.Rgb16f;
            else
                MotionVectorFormat = (long)InternalFormat.Rgba16f;

            DepthFormat = (long)InternalFormat.DepthComponent16;
        }

        public unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {
            if (_renderer.RenderTarget is not IGlRenderTargetFB fbTarget)
                throw new NotSupportedException();

            var colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;

            var glTex = GlTexture.Attach(_renderer.GL, colorTex);

            fbTarget.FrameBuffer.Attach(glTex, FramebufferAttachment.ColorAttachment2, true);

            fbTarget.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1, DrawBufferMode.ColorAttachment2);
        }

        public long MotionVectorFormat { get; }

        public long DepthFormat { get; }

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
