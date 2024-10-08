
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
        private readonly Texture2D? _depthTextureCopy;
        private string _allLightsHash = "";
        private DirectionalLight? _light;
        private Camera? _oldCamera;
        private long _lightVersion = -1;
        private long _layerVersion = -1;
        private long _recLayerVersion = -1;
        private long _updateFrame;
        private readonly ShadowMapMode _mode;

        private readonly OrtoCamera _lightCamera;

        public GlShadowPass(OpenGLRender renderer)
            : base(renderer)
        {

            UseShadowSampler = false;

            //renderer.Options.ShadowMap.Mode = ShadowMapMode.VSM;

            _mode = renderer.Options.ShadowMap.Mode;

            _lightCamera = new OrtoCamera();

            _depthTexture = new Texture2D
            {
                BorderColor = Color.White,
                WrapT = WrapMode.ClampToBorder,
                WrapS = WrapMode.ClampToBorder,
                Width = _renderer.Options.ShadowMap.Size,
                Height = _renderer.Options.ShadowMap.Size,
                Format = TextureFormat.Depth24Float,
                MinFilter = ScaleFilter.Nearest,
                MagFilter = ScaleFilter.Nearest,
                MipLevelCount = 1
            };

            if (_mode == ShadowMapMode.VSM)
            {
                _depthTextureCopy = new Texture2D
                {
                    BorderColor = Color.White,
                    WrapT = WrapMode.ClampToBorder,
                    WrapS = WrapMode.ClampToBorder,
                    Width = _renderer.Options.ShadowMap.Size,
                    Height = _renderer.Options.ShadowMap.Size,
                    Format = TextureFormat.RgbFloat16,
                    MinFilter = ScaleFilter.Linear,
                    MagFilter = ScaleFilter.Linear,
                    //MaxAnisotropy = 4,
                    MipLevelCount = 1
                };
            }
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return null;
        }

        protected override void Initialize()
        {
            var glDeptTex = _depthTexture.ToGlTexture();
            var glColorTex = _depthTextureCopy?.ToGlTexture();

            _frameBuffer = new GlTextureFrameBuffer(_renderer.GL);
            _frameBuffer.Configure(glColorTex, glDeptTex, 1);

            if (UseShadowSampler)
            {
                glDeptTex.Bind();
                _renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
                _renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
                glDeptTex.Unbind();
            }

            base.Initialize();
        }

        protected override ShaderMaterial CreateMaterial()
        {
            if (_mode == ShadowMapMode.VSM)
                return new VsmShadowMaterial();
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

        public override void Render(Camera camera)
        {
            UpdateLight();

            base.Render(camera);
        }

        protected void UpdateCamera(CastShadowsLayer castLayer)
        {
            _lightCamera.CreateViewFromDirection(_light!.Direction, Vector3.UnitY);

            var frustumPoints = _renderer.UpdateContext.Camera!.FrustumPoints();
            var frustumLightBounds = frustumPoints.ComputeBounds(_lightCamera.View);

            var receiveBounds = castLayer.WorldBounds;
            var receiveBoundsLight = receiveBounds.Points.ComputeBounds(_lightCamera.View);

            if (frustumLightBounds.Intersects(receiveBoundsLight, out var lightBounds))
            {
                _lightCamera.Far = -lightBounds.Min.Z;
                _lightCamera.Near = 0.01f;
                _lightCamera.SetViewArea(lightBounds.Min.X, lightBounds.Max.X, lightBounds.Min.Y, lightBounds.Max.Y);
            }
        }

        protected override bool BeginRender(Camera camera)
        {
            var shadowRenderLayer = SelectLayers().First();
            var scene = _renderer.UpdateContext.Camera!.Scene!;
            var recLayer = scene.EnsureLayer<ReceiveShadowsLayer>();
            var castLayer = scene.EnsureLayer<CastShadowsLayer>();
            var frame = scene.App!.RenderContext.Frame;

            if (_light == null || shadowRenderLayer.Content.ShaderContents.Count == 0 || !recLayer.Content.Any())
                return false;

            if (_updateFrame == frame)
                return false;

            /*
            if (_light!.Version == _lightVersion && 
                shadowLayer.Version == _layerVersion &&
                recLayer.Version == _recLayerVersion)
                return false;

            Log.Debug(this, "Rendering shadow map for light '{0}'...", _light!.Name); 

            _layerVersion = shadowRenderLayer.Version;
            _lightVersion = _light.Version;
            _recLayerVersion = recLayer.Version;
             */

            _updateFrame = frame;

            _frameBuffer!.Bind();

            _renderer.State.SetWriteDepth(true);

            if (_mode == ShadowMapMode.VSM)
            {
                _renderer.State.SetWriteColor(true);
                _renderer.State.SetClearColor(Color.White);
            }
            else
                _renderer.State.SetWriteColor(false);

            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetView(new Rect2I(0, 0, _depthTexture!.Width, _depthTexture.Height));
            _renderer.State.SetCullFace(TriangleFace.Front);

            _renderer.GL.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            UpdateCamera(castLayer);

            _oldCamera = _renderer.UpdateContext.Camera;
            _renderer.UpdateContext.Camera = _lightCamera;

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderer.UpdateContext.Camera = _oldCamera;
            _renderer.State.SetCullFace(TriangleFace.Back);

            if (_mode == ShadowMapMode.VSM)
                _frameBuffer!.Invalidate(InvalidateFramebufferAttachment.DepthAttachment);

            _frameBuffer!.Unbind();

            base.EndRender();
        }

        public DirectionalLight? Light => _light;

        public Texture2D? DepthTexture => _mode == ShadowMapMode.VSM ? _depthTextureCopy : _depthTexture;

        public Camera LightCamera => _lightCamera;

        public bool UseShadowSampler { get; set; }
    }
}
