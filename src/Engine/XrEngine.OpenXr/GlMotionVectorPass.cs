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
        protected GlTextureRenderTarget? _renderTarget;
        protected GlTexture? _glColorImage;
        protected GlTexture? _glDepthImage;
        protected int _bindEye;
        protected int _activeEye;

        public GlMotionVectorPass(OpenGLRender renderer, XrApp xrApp, int bindEye)
            : base(renderer)
        {
            _xrApp = xrApp;
            _renderTarget = new GlTextureRenderTarget(_renderer.GL);
            _bindEye = bindEye;
        }


        public unsafe void SetTarget(SwapchainImageBaseHeader* colorImg, SwapchainImageBaseHeader* depthImg)
        {
            var sampleCount = _xrApp.RenderOptions.SampleCount;

            var colorTex = ((SwapchainImageOpenGLKHR*)colorImg)->Image;
            var depthTex = ((SwapchainImageOpenGLKHR*)depthImg)->Image;

            _glColorImage = GlTexture.Attach(_renderer.GL, colorTex, sampleCount);
            _glDepthImage = GlTexture.Attach(_renderer.GL, depthTex, sampleCount);
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool BeginRender(Camera camera)
        {
            if (_glColorImage == null || _glDepthImage == null || _renderTarget == null)
                return false;

            var sampleCount = _xrApp.RenderOptions.SampleCount;

            _activeEye = _bindEye == -1 ? camera.ActiveEye : _bindEye;

            camera.ActiveEye = _activeEye;

            if (_glColorImage.Depth > 1)
            {
                _renderTarget.FrameBuffer.Configure(
                _glColorImage, (uint)_activeEye,
                _glDepthImage, (uint)_activeEye,
                sampleCount);
            }
            else
            {
                _renderTarget.FrameBuffer.Configure(_glColorImage, _glDepthImage, sampleCount);
            }

            _renderTarget.Begin(camera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(new Color(0, 0, 0, 1));
            _renderer.State.SetView(new Rect2I(0, 0, _glColorImage.Width, _glColorImage.Height));

            _renderer.GL.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderTarget?.End(false);

            if (_bindEye != -1 || _activeEye == 1)
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

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main).Take(1);
        }

        public override void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            base.Dispose();
        }
    }
}
