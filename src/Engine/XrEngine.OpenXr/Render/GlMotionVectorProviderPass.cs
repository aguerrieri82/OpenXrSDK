using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using Silk.NET.Vulkan;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProviderPass : IXrMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        private readonly GlMotionVectorPass _pass;

        public GlMotionVectorProviderPass(EngineApp app, OpenGLRender renderer, GlMotionVectorPass pass)
        {
            _renderer = renderer;
            _app = app;
            _pass = pass;

            if (_renderer.UseAngle)
            {
                MotionVectorFormat = (int)Format.R16G16B16A16Sfloat;
                DepthFormat = (int)Format.D16Unorm;
            }
            else
            {
                if (XrPlatform.IsEditor)
                    MotionVectorFormat = (int)InternalFormat.Rgb16f;
                else
                    MotionVectorFormat = (int)InternalFormat.Rgba16f;

                DepthFormat = (int)InternalFormat.DepthComponent16;
            }

            IsActive = true;

            Context.Implement<IXrMotionVectorProvider>(this);
        }

        public unsafe void UpdateMotionVectors(in SpaceWarpData spData, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {
            _pass.SetTargets(spData, colorImg, depthImg, MotionVectorFormat);
        }

        public int MotionVectorFormat { get; }

        public int DepthFormat { get; }

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
