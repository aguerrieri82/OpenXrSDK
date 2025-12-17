#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using Silk.NET.OpenXR;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class GlMotionVectorPass : GlBaseSingleMaterialPass
    {
        protected readonly XrApp _xrApp;
        protected IGlRenderTarget? _renderTarget;
        protected GlTexture? _glColorImage;
        protected GlTexture? _glDepthImage;
        protected int _boundEye;
        protected int _activeEye;

        public GlMotionVectorPass(OpenGLRender renderer, XrApp xrApp, int boundEye = -1, bool multiView = false)
            : base(renderer)
        {
            _xrApp = xrApp;

            if (multiView)
                _renderTarget = new GlMultiViewRenderTarget(_gl);
            else
                _renderTarget = new GlTextureRenderTarget(_gl);

            _boundEye = boundEye;
        }


        public unsafe void SetTargets(SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg)
        {
            var sampleCount = _xrApp.RenderOptions.SampleCount;

            var colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
            var depthTex = ((SwapchainImageOpenGLKHR*)depthImg)->Image;

            _glColorImage = GlTexture.Attach(_gl, colorTex, sampleCount);
            _glDepthImage = GlTexture.Attach(_gl, depthTex, sampleCount);
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            var effect = MotionVectorEffect.Instance;
            effect.WriteDepth = drawMaterial.WriteDepth;
            effect.UseDepth = drawMaterial.UseDepth;
            effect.DoubleSided = drawMaterial.DoubleSided;
            effect.WriteColor = drawMaterial.WriteColor;
            return base.UpdateProgram(updateContext, drawMaterial);
        }

        protected override bool BeginRender(Camera camera)
        {
            if (_glColorImage == null || _glDepthImage == null || _renderTarget == null || camera.Eyes == null)
                return false;

            _activeEye = _boundEye == -1 ? camera.ActiveEye : _boundEye;

            if (_renderTarget is GlMultiViewRenderTarget mv)
                mv.FrameBuffer.Configure(_glColorImage, _glDepthImage, 1);

            else if (_renderTarget is GlTextureRenderTarget tex)
            {
                if (_glColorImage.Depth > 1)
                    tex.FrameBuffer.Configure(_glColorImage, (uint)_activeEye, _glDepthImage!, (uint)_activeEye, 1);
                else
                    tex.FrameBuffer.Configure(_glColorImage, _glDepthImage, 1);
            }

            _renderTarget.Begin(camera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(new Color(0, 0, 0, 1));


            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderTarget?.End(false);

            if (_boundEye != -1 || _activeEye == 1)
            {
                _glColorImage = null;
                _glDepthImage = null;
            }

            base.EndRender();
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return MotionVectorEffect.Instance;
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Opaque).Take(1);
        }

        public override void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            base.Dispose();
        }
    }
}
