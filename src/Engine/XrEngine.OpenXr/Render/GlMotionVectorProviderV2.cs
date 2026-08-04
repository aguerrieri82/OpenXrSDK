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
    internal class GlMotionVectorProviderV2 : IXrMotionVectorProvider, IMotionVectorProvider
    {
        readonly OpenGLRender _renderer;
        readonly EngineApp _app;
        protected Texture2D? _texture;

        public GlMotionVectorProviderV2(EngineApp app, OpenGLRender renderer)
        {
            _renderer = renderer;
            _app = app;

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
        }

        public unsafe void UpdateMotionVectors(in SpaceWarpData spData, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, XrRenderMode mode)
        {

            uint colorTex;
            uint depthTex;

            if (_renderer.UseAngle)
            {
                var colorVkImage = (nint)((SwapchainImageVulkanKHR*)colorImg)->Image;
        
                var ctx = Context.Require<AngleVulkanContext>();

                colorTex = ctx.AttachVulkanImage(
                    colorVkImage,
                    MotionVectorFormat,
                    (uint)spData.ColorSize.Width,
                    (uint)spData.ColorSize.Height,
                    2,1,1,
                    ImageUsageFlags.ColorAttachmentBit |ImageUsageFlags.SampledBit,
                    TextureTarget.Texture2DArray).Texture;
            }
            else
            {
                colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
                depthTex = ((SwapchainImageOpenGLKHR*)depthImg)->Image;
            }

            var colorGlTex = GlTexture.Attach(_renderer.GL, colorTex);

            colorGlTex.Clear(Color.Black);

            _texture = (Texture2D)colorGlTex.ToEngineTexture();
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
