using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProvider : IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        private readonly GlMotionVectorPass _pass;

        public GlMotionVectorProvider(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;
            _pass = _renderer.Pass<GlMotionVectorPass>() ?? throw new NotSupportedException();

            MotionVectorFormat = (long)InternalFormat.Rgba16f;
            DepthFormat = (long)InternalFormat.DepthComponent16;

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
