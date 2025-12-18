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
        protected Dictionary<uint, IGlRenderTarget> _targets = [];
        protected bool _multiView;

        public GlMotionVectorPass(OpenGLRender renderer, XrApp xrApp, int boundEye = -1, bool multiView = false)
            : base(renderer)
        {
            _xrApp = xrApp;

            _multiView = multiView;

            _boundEye = boundEye;
        }


        public unsafe void SetTargets(SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg)
        {
            uint sampleCount = _xrApp.RenderOptions.SampleCount;

            uint colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
            uint depthTex = ((SwapchainImageOpenGLKHR*)depthImg)->Image;

            _glColorImage = GlTexture.Attach(_gl, colorTex, sampleCount);
            _glDepthImage = GlTexture.Attach(_gl, depthTex, sampleCount);


            uint targetId = colorTex + depthTex << 16;
            if (!_targets.TryGetValue(targetId, out _renderTarget))
            {
                if (_multiView)
                {
                    GlMultiViewRenderTarget mv = new GlMultiViewRenderTarget(_gl);
                    mv.FrameBuffer.Configure(_glColorImage, _glDepthImage, 1);
                    _renderTarget = mv;
                }
                else
                {
                    GlTextureRenderTarget tex = new GlTextureRenderTarget(_gl);
                    if (_glColorImage.Depth > 1)
                        tex.FrameBuffer.Configure(_glColorImage, (uint)_activeEye, _glDepthImage!, (uint)_activeEye, 1);
                    else
                        tex.FrameBuffer.Configure(_glColorImage, _glDepthImage, 1);
                    _renderTarget = tex;
                }
                _targets[targetId] = _renderTarget;
            }
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            MotionVectorEffect effect = MotionVectorEffect.Instance;
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
                _renderTarget = null;
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
