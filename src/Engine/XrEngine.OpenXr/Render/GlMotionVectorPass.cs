#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;
using XrMath;
using static XrEngine.Filament.FilamentLib;

namespace XrEngine.OpenXr
{
    public class GlMotionVectorPass : GlBaseSingleMaterialPass
    {
        protected readonly XrApp _xrApp;
        protected IGlRenderTarget? _renderTarget;
        protected GlFrameBufferPool _pool;
        protected bool _multiView;
        private uint _colorTex;
        private uint _depthTex;
        private Camera? _oldCamera;

        public GlMotionVectorPass(OpenGLRender renderer, XrApp xrApp, bool multiView = false)
            : base(renderer)
        {
            _xrApp = xrApp;

            _multiView = multiView;
            _pool = new GlFrameBufferPool(renderer.GL, multiView);
        }

        public unsafe void SetTargets(SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg)
        {
            _colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
            _depthTex = ((SwapchainImageOpenGLKHR*)depthImg)->Image;
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            return true;
            //return draw.Object is not ISkinnedMesh;
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            var effect = MotionVectorEffect.Instance;

            effect.WriteDepth = drawMaterial.WriteDepth;
            effect.UseDepth = drawMaterial.UseDepth;
            effect.DoubleSided = drawMaterial.DoubleSided;
            effect.WriteColor = drawMaterial.WriteColor;

            if (drawMaterial is ShaderMaterial mat)
                effect.HasSkin = mat.HasSkin;

            return base.UpdateProgram(updateContext, drawMaterial);
        }

        protected override bool BeginRender(Camera camera)
        {
            if (camera.Eyes == null || _colorTex == 0)
                return false;

            _renderTarget = _pool.GetRenderTarget(_colorTex, _depthTex, 1, camera.ActiveEye);

            _renderer.UpdateContext.PassCamera = camera.Clone();

            _oldCamera = camera;

            var newCamera = camera.Clone();

            _renderer.UpdateContext.PassCamera = newCamera;

            _renderTarget.Begin(newCamera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(new Color(0, 0, 0, 0));

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));


            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            MotionVectorEffect.Instance.EndPass(_renderer.UpdateContext.PassCamera!);

            _renderTarget?.End(false);

            _renderer.UpdateContext.PassCamera = _oldCamera;

            if (_oldCamera!.ActiveEye == -1 || _oldCamera.ActiveEye == 1)
            {
                _colorTex = 0;
                _depthTex = 0;
            }

            base.EndRender();
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
