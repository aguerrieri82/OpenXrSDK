#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.OpenGL
{

    [StructLayout(LayoutKind.Explicit, Size = 304)]
    public struct SceneMatrices
    {
        [FieldOffset(0)]
        public Matrix4x4 ViewProj1;

        [FieldOffset(64)]
        public Matrix4x4 ViewProj2;

        [FieldOffset(128)]
        public Vector3 Position1;

        [FieldOffset(144)]
        public Vector3 Position2;

        [FieldOffset(160)]
        public Matrix4x4 ViewProjInv1;

        [FieldOffset(224)]
        public Matrix4x4 ViewProjInv2;

        [FieldOffset(288)]
        public float FarPlane;

    }

    public class GlMultiViewShaderHandler : IShaderHandler
    {
        protected SceneMatrices _matrices = new();

        public bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return ctx.LastGlobalUpdate?.ShaderHandlers == null || !ctx.LastGlobalUpdate.ShaderHandlers.Contains(this);
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.AddFeature("MULTI_VIEW");

            if (bld.Context.Stage == UpdateShaderStage.Shader)
            {
                bld.LoadBuffer<SceneMatrices>((ctx, ref update) =>
                {
                    update.Value = _matrices;
                    return true;

                }, UniformsSlots.MultiView, BufferStore.Shader);
            }
        }

        public void SetCamera(Camera camera)
        {
            var eyes = camera.Eyes;

            if (eyes == null)
                return;

            _matrices.ViewProj1 = eyes[0].ViewProj;
            _matrices.ViewProj2 = eyes[1].ViewProj;
            _matrices.ViewProjInv1 = eyes[0].ViewProjInv;
            _matrices.ViewProjInv2 = eyes[1].ViewProjInv;
            _matrices.Position1 = eyes[0].World.Translation;
            _matrices.Position2 = eyes[1].World.Translation;
            _matrices.FarPlane = camera.Far;
        }

        public static readonly GlMultiViewShaderHandler Instance = new();
    }

    public class GlMultiViewRenderTarget : IGlRenderTargetFB
    {
        protected GlMultiViewFrameBuffer _frameBuffer;

        protected readonly GL _gl;

        public GlMultiViewRenderTarget(GL gl)
        {
            _frameBuffer = new GlMultiViewFrameBuffer(gl);
            _gl = gl;
        }

        public void Begin(Camera camera)
        {
            if (RenderSize.Width == 0 || RenderSize.Height == 0)
                camera.ViewSize = _frameBuffer.Size;
            else
                camera.ViewSize = RenderSize;

            GlState.Current.SetView(new Rect2I(camera.ViewSize));

            _frameBuffer.BindDraw();

            GlMultiViewShaderHandler.Instance.SetCamera(camera);

            OpenGLRender.Current!.Begin(this);
        }

        public void End(bool discardDepth)
        {
            if (discardDepth)
                _frameBuffer.Invalidate(InvalidateFramebufferAttachment.DepthStencilAttachment);

            _frameBuffer.Unbind();
        }

        public GlTexture? QueryTexture(FramebufferAttachment attachment)
        {
            return _frameBuffer.QueryTexture(attachment);
        }

        public void Dispose()
        {
            _frameBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public GlMultiViewFrameBuffer FrameBuffer => _frameBuffer;

        IGlFrameBuffer IGlFrameBufferProvider.FrameBuffer => _frameBuffer;

        public IShaderHandler? ShaderHandler => GlMultiViewShaderHandler.Instance;

        public GlRenderTargetFlags Flags { get; set; }

        public int ShadingRate { get; set; }

        public Size2I RenderSize { get; set; }

        public Rect2I[]? ClipRegions { get; set; }
    }
}
