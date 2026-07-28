using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProvider : IXrMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        private readonly GlMotionVectorPass _pass;

        public GlMotionVectorProvider(EngineApp app, OpenGLRender renderer, GlMotionVectorPass pass)
        {
            _renderer = renderer;
            _app = app;
            _pass = pass;

            if (XrPlatform.IsEditor)
                MotionVectorFormat = (long)InternalFormat.Rgb16f;
            else
                MotionVectorFormat = (long)InternalFormat.Rgba16f;

            DepthFormat = (long)InternalFormat.DepthComponent16;
            IsActive = true;

            Context.Implement<IXrMotionVectorProvider>(this);
        }

        public unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {
            _pass.SetTargets(colorImg, depthImg);
        }

        public long MotionVectorFormat { get; }

        public long DepthFormat { get; }

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
