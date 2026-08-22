#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;
using XrMath;
using OpenXr.Framework.Angle;

namespace XrEngine.OpenXr
{
    public class GlMotionVectorPass : GlBaseSingleMaterialPass
    {
        protected readonly XrApp _xrApp;
        protected IGlRenderTarget? _renderTarget;
        protected GlRenderTargetPool _pool;
        protected readonly bool _isEditor;
        protected bool _multiView;
        protected uint _colorTex;
        protected AngleVulkanContext? _vulkanCtx;
        protected readonly GlTexture? _debugColor;

        public GlMotionVectorPass(OpenGLRender renderer, XrApp xrApp, bool multiView = false)
            : base(renderer)
        {
            _xrApp = xrApp;

            _multiView = multiView;

            _pool = new GlRenderTargetPool(renderer.GL, multiView)
            {
                Name = "Motion Vectors"
            };

            _isEditor = XrPlatform.IsEditor;

            _flags = GlRenderPassFlags.CustomCamera;

            if (_isEditor)
            {
                _debugColor = new GlTexture(renderer.GL);
                _debugColor.Allocate(1024, 1024, 2, TextureFormat.RgbaFloat16);
            }

            Priority = -1;
        }

        public unsafe void SetTargets(XrSwapchain swapchain, SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg, int colorFormat)
        {
            if (_renderer.Features.IsAngle)
            {
                _vulkanCtx ??= Context.Require<AngleVulkanContext>();
                _colorTex = _vulkanCtx.AttachVulkanImage(colorImg, swapchain).Texture;
            }
            else
                _colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            return true;
        }

        protected override bool CanDraw(Material drawMaterial)
        {
            if (drawMaterial.Is(EngineObjectFlags.Secondary) || !drawMaterial.WriteColor)
                return false;
            return true;
        }

        protected override UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext updateContext, Material drawMaterial)
        {
            var effect = MotionVectorEffect.Instance;

            effect.WriteDepth = drawMaterial.WriteDepth;
            effect.UseDepth = drawMaterial.UseDepth;
            effect.DoubleSided = drawMaterial.DoubleSided;

            if (drawMaterial is ShaderMaterial mat)
                effect.UseSkin = mat.UseSkin;

            return base.UpdateProgram(instance, updateContext, drawMaterial);
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            var camera = ctx.PassCamera!;

            if (camera.Eyes == null || _colorTex == 0)
                return false;

            if (_isEditor)
                _renderTarget = _pool.GetRenderTarget(_debugColor!, 0, 1, camera.ActiveEye);
            else
                _renderTarget = _pool.GetRenderTarget(_colorTex, 0, 1, camera.ActiveEye);

            _renderTarget.ShadingRate = 2;

            _renderTarget.Begin(camera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(new Color(0, 0, 0, 0));
            _renderer.State.Commit();

            _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            return base.BeginRender(ctx);
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            var camera = ctx.PassCamera!;

            _renderTarget?.End(false);

            if (camera.ActiveEye == -1 || camera.ActiveEye == 1)
            {
                MotionVectorEffect.Instance.EndPass(camera);
                _colorTex = 0;
            }

            base.EndRender(ctx);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return MotionVectorEffect.Instance;
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => (a.Type & GlLayerType.Color) != 0);
        }

        public override void Dispose()
        {
            _pool?.Dispose();
            _renderTarget = null;
            base.Dispose();
        }
    }
}
