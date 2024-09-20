
using System.Numerics;
using XrMath;

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlShadowPass : GlBaseSingleMaterialPass
    {
        private GlTextureFrameBuffer? _frameBuffer;
        private readonly Texture2D _depthTexture;
        private string _allLightsHash = "";
        private DirectionalLight? _light;
        private Camera? _oldCamera;
        private long _lightVersion = -1;
        private long _layerVersion = -1;
        private readonly OrtoCamera _lightCamera;

        public GlShadowPass(OpenGLRender renderer)
            : base(renderer)
        {
            _lightCamera = new OrtoCamera();
            _lightCamera.SetViewArea(-4, 4, -4, 4);

            _depthTexture = new Texture2D
            {
                BorderColor = Color.White,
                WrapT = WrapMode.ClampToBorder,
                WrapS = WrapMode.ClampToBorder,
                Width = _renderer.Options.ShadowMap.Size,
                Height = _renderer.Options.ShadowMap.Size,
                Format = TextureFormat.Depth24Stencil8,
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                MaxLevels = 1
            };
        }

        protected override void Initialize()
        {
            _frameBuffer = new GlTextureFrameBuffer(_renderer.GL);
            _frameBuffer.Configure(null, _renderer.GetGlResource(_depthTexture));

            base.Initialize();
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new DepthOnlyMaterial();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.CastShadow);
        }

        protected bool UpdateLight()
        {
            if (_allLightsHash != _renderer.UpdateContext.LightsHash)
            {
                _allLightsHash = _renderer.UpdateContext.LightsHash!;

                _light = _renderer.UpdateContext.Lights?
                    .OfType<DirectionalLight>()
                    .FirstOrDefault(a => a.CastShadows);

                IsEnabled = _light != null;
            }

            return IsEnabled;
        }

        protected override bool BeginRender()
        {
            if (!UpdateLight())
                return false;

            var shadowLayer = SelectLayers().First();

            if (shadowLayer.Content.ShaderContents.Count == 0)
                return false;

            if (_light!.Version == _lightVersion && shadowLayer.Version == _layerVersion)
                return false;

            Log.Debug(this, "Rendering shadow map for light '{0}'...", _light!.Name);      

            _layerVersion = shadowLayer.Version;
            _lightVersion = _light.Version; 

            _oldCamera = _renderer.UpdateContext.Camera;

            _frameBuffer!.Bind();

            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetView(new Rect2I(0, 0, _depthTexture!.Width, _depthTexture.Height));
            _renderer.State.SetCullFace(TriangleFace.Front);    

            _renderer.GL.Clear((uint)ClearBufferMask.DepthBufferBit);

            _lightCamera.CreateViewFromDirection(_light!.Direction, Vector3.UnitY);
            _renderer.UpdateContext.Camera = _lightCamera;

            return base.BeginRender();
        }

        protected override void EndRender()
        {
            _renderer.UpdateContext.ShadowLightCamera = _lightCamera;
            _renderer.UpdateContext.ShadowMap = _depthTexture;
            _renderer.UpdateContext.Camera = _oldCamera;
            _renderer.State.SetCullFace(TriangleFace.Back);

            _frameBuffer!.Unbind();

            base.EndRender();
        }

        public Texture2D? DepthTexture => _depthTexture;

        public Camera LightCamera => _lightCamera;
    }
}
