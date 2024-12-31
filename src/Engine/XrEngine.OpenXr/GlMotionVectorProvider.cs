using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProvider : IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        private readonly GlMotionVectorPass[] _passes;

        public GlMotionVectorProvider(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;
            _passes = _renderer.Passes<GlMotionVectorPass>().ToArray();
        }

        public unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader*[] colorImgs, SwapchainImageBaseHeader*[] depthImgs, XrRenderMode mode)
        {
            for (var i = 0; i < _passes.Length; i++)
            {
                _passes[i].SetTarget(colorImgs.Length == 1 ? colorImgs[0] : colorImgs[i],
                                     depthImgs.Length == 1 ? depthImgs[0] : depthImgs[i]);
            }
        }

        public long MotionVectorFormat => (long)InternalFormat.Rgba16f;

        public long DepthFormat => (long)InternalFormat.DepthComponent24;

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
