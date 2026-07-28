#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;
using XrEngine.Helpers;
using System.Numerics;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProviderV2 : IXrMotionVectorProvider, IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        protected Texture2D? _texture;

        public GlMotionVectorProviderV2(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;

            if (XrPlatform.IsEditor)
                MotionVectorFormat = (long)InternalFormat.Rgb16f;
            else
                MotionVectorFormat = (long)InternalFormat.Rgba16f;

            DepthFormat = (long)InternalFormat.DepthComponent16;
            IsActive = true;

            renderer.UpdateContext.MotionVectorProvider = this;

            Context.Implement<IXrMotionVectorProvider>(this);
            Context.Implement<IMotionVectorProvider>(this);
        }

        public unsafe void UpdateMotionVectors(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {
            if (_renderer.RenderTarget is not IGlRenderTargetFB fbTarget)
                return;

            var colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;

            var glTex = GlTexture.Attach(_renderer.GL, colorTex);

            _texture = (Texture2D)glTex.ToEngineTexture();
        }

        public void Swap(Camera camera, IEnumerable<Object3D> objects)
        {
            foreach (var obj in objects)
                obj.SetProp(EngineProps.MotionVectorPrev, obj.WorldMatrix);

            Matrix4x4[] viewProj = camera.Eyes != null && camera.Eyes.Length == 2 ?
                    [camera.Eyes[0].ViewProj, camera.Eyes[1].ViewProj] :
                    [camera.ViewProjection];

            camera.SetProp(EngineProps.MotionVectorPrev, viewProj);
        }

        public Matrix4x4? GetPrevMatrix(Object3D model)
        {
            var matrice = model.GetProp<Matrix4x4>(EngineProps.MotionVectorPrev);
            return matrice;
        }

        public Matrix4x4[]? GetPrevMatrix(Camera camera)
        {
            var matrices = camera.GetProp<Matrix4x4[]>(EngineProps.MotionVectorPrev);
            return matrices;
        }

        public Texture2D? Texture => _texture;

        public long MotionVectorFormat { get; }

        public long DepthFormat { get; }

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
