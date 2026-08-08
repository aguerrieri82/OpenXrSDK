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
using XrMath;
using Silk.NET.Vulkan;
using OpenXr.Framework.Angle;

namespace XrEngine.OpenXr
{
    internal class GlMotionVectorProviderShared : IXrMotionVectorProvider, IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        protected Texture2D? _texture;
        protected readonly AngleVulkanContext? _context;
        protected GlMultiViewFrameBuffer _testFb;
        private AngleVulkanContext _vulkanCtx;

        public GlMotionVectorProviderShared(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;
            _testFb = new GlMultiViewFrameBuffer(renderer.GL);

            if (renderer.UseAngle)
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

            renderer.UpdateContext.MotionVectorProvider = this;

            Context.Implement<IXrMotionVectorProvider>(this);
            Context.Implement<IMotionVectorProvider>(this);

            if (_renderer.UseAngle)
                _context = Context.Require<AngleVulkanContext>();
        }

        public unsafe void UpdateMotionVectors(XrSwapchain swapchain, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {
            uint colorTex;

            if (_renderer.UseAngle)
            {
                _vulkanCtx ??= Context.Require<AngleVulkanContext>();

                colorTex = _vulkanCtx.AttachVulkanImage(colorImg, swapchain).Texture;
            }
            else
                colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;

            var colorGlTex = GlTexture.Attach(_renderer.GL, colorTex);

            colorGlTex.Clear(Color.Black);

            _texture = (Texture2D)colorGlTex.ToEngineTexture();
        }

        public void Begin()
        {
            if (_renderer.UseAngle)
                _context!.AcquireTexture((uint)_texture!.Handle);
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

        public int MotionVectorFormat { get; }

        public int DepthFormat { get; }

        public float Near => _app.ActiveScene?.ActiveCamera?.Near ?? 0.1f;

        public float Far => _app.ActiveScene?.ActiveCamera?.Far ?? 100f;

        public bool IsActive { get; set; }
    }
}
