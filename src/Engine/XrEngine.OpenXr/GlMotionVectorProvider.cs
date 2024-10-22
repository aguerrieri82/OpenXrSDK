using glTFLoader.Schema;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenGL;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProvider : IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;


        public GlMotionVectorProvider(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app; 
        }

        public unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {

            for (var i = 0; i < projViews.Length; i++)
            {
                var transform = XrCameraTransform.FromView(projViews[i], Near, Far);
                var pose = transform.Transform.ToPose(); 

            }

            foreach (var pass in _renderer.Passes<GlMotionVectorPass>())
                pass.SetTarget(colorImg, depthImg);
        }

        public long MotionVectorFormat => (long)InternalFormat.Rgba16f;

        public long DepthFormat => (long)InternalFormat.DepthComponent24;

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;   

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;
    }
}
