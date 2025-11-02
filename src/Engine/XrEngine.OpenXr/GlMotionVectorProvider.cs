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
            //TODO: In case not using array, we must set all tragets toghter,
            //since the pass is single and the right image is determined by the active eye of the camera.

            for (var i = 0; i < _passes.Length; i++)
            {
                int ix;
                if (_passes.Length == 1)
                {
                    if (colorImgs.Length == 1)
                        ix = 0;
                    else
                        ix = _renderer.UpdateContext.MainCamera!.ActiveEye;
                }
                else
                    ix = i;

                _passes[i].SetTargets(colorImgs[ix], depthImgs[ix]);
            }
        }

        public long MotionVectorFormat => (long)InternalFormat.Rgba16f;

        public long DepthFormat => (long)InternalFormat.DepthComponent16; //If we use DepthComponent24, doesn't work in Quest 3.

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
