
using System.Numerics;
using XrMath;
using System.Diagnostics;




#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlShadowPass : GlBaseSingleMaterialPass, IShadowMapProvider
    {

        private GlTextureFrameBuffer? _frameBuffer;
        private readonly Texture2D _depthTexture;
        private readonly Texture2D? _vcmMomentsTex;
        private readonly Texture2D? _vcmTempTex;
        private string _allLightsHash = "";
        private DirectionalLight? _light;
        private Camera? _oldCamera;
        private long _recLayerVersion = -1;
        private long _castLayerVersion = -1;
        private long _updateFrame;
        private long _lightVersion;
        private readonly ShadowMapMode _mode;

        private readonly OrtoCamera _lightCamera;

        public GlShadowPass(OpenGLRender renderer)
            : base(renderer)
        {

            UseShadowSampler = false;

            _mode = renderer.Options.ShadowMap.Mode;

            _lightCamera = new OrtoCamera();
            _lightCamera.Name = "Shadow";

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
                MipLevelCount = 1,
                Name = "Depth"
            };

            if (_mode == ShadowMapMode.VSM)
            {
                _vcmMomentsTex = new Texture2D
                {
                    BorderColor = Color.White,
                    WrapT = WrapMode.ClampToBorder,
                    WrapS = WrapMode.ClampToBorder,
                    Width = _renderer.Options.ShadowMap.Size,
                    Height = _renderer.Options.ShadowMap.Size,
                    Format = TextureFormat.RgbaFloat32,
                    MinFilter = ScaleFilter.Linear,
                    MagFilter = ScaleFilter.Linear,
                    MaxAnisotropy = 16.0f,
                    MipLevelCount = 6,
                    Name = "Moments"
                };


                _vcmTempTex = new Texture2D
                {
                    BorderColor = Color.White,
                    WrapT = WrapMode.ClampToBorder,
                    WrapS = WrapMode.ClampToBorder,
                    Width = _renderer.Options.ShadowMap.Size,
                    Height = _renderer.Options.ShadowMap.Size,
                    Format = TextureFormat.RgbaFloat32,
                    MinFilter = ScaleFilter.LinearMipmapLinear,
                    MagFilter = ScaleFilter.Linear,
                    MaxAnisotropy = 16.0f,
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
            var glColorTex = _vcmMomentsTex?.ToGlTexture();

            _frameBuffer = new GlTextureFrameBuffer(_gl);
            _frameBuffer.Configure(glColorTex, glDeptTex, 1);

            if (UseShadowSampler)
            {
                glDeptTex.Bind();
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
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

        protected override IEnumerable<IGlLayer> SelectLayers()
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

        public override void Render(RenderContext ctx)
        {
            UpdateLight();

            base.Render(ctx);
        }

        protected void UpdateCamera(CastShadowsLayer castLayer, ReceiveShadowsLayer recvLayer)
        {
            Debug.Assert(_light != null);

            _lightCamera.CreateViewFromDirection(_light.Direction, Vector3.UnitY);

            Bounds3 sourceBounds;
            Bounds3 finalBounds;

            var frustumPoints = _renderer.UpdateContext.PassCamera!.FrustumPoints();
            var frustumLightBounds = frustumPoints.ComputeBounds(_lightCamera.View);

            var receiveBounds = recvLayer.WorldBounds;
            var receiveBoundsLight = receiveBounds.Points.ComputeBounds(_lightCamera.View);

            var castBounds = castLayer.WorldBounds;
            var castBoundsLight = receiveBounds.Points.ComputeBounds(_lightCamera.View);

            var options = _renderer.Options.ShadowMap;

            if (options.IsCasterMode)
                sourceBounds = castBoundsLight;
            else
                sourceBounds = receiveBoundsLight;

            sourceBounds.Min -= options.Expand;
            sourceBounds.Max += options.Expand;

            if (options.UseFrustumIntersect)
            {
                if (!frustumLightBounds.Intersects(sourceBounds, out finalBounds))
                    return;
            }
            else
                finalBounds = sourceBounds;

            var zNear = Math.Max(0.05f, -finalBounds.Max.Z);
            var zFar = Math.Max(zNear + 0.01f, -finalBounds.Min.Z);

            _lightCamera.Near = Math.Max(0.05f, zNear - 1.0f);
            _lightCamera.Far = zFar + 1.0f;

            _lightCamera.SetViewArea(finalBounds.Min.X, finalBounds.Max.X, finalBounds.Min.Y, finalBounds.Max.Y);
        }

        protected override bool BeginRender(Camera camera)
        {
            //Debug.Assert(camera.Scene != null);
            var shadowRenderLayer = SelectLayers().First();
            var scene = shadowRenderLayer.Scene!;
            var recLayer = scene.EnsureLayer<ReceiveShadowsLayer>();
            var castLayer = scene.EnsureLayer<CastShadowsLayer>();
            var frame = scene.App!.RenderContext.Frame;

            if (_light == null)
                return false;

            if (!_renderer.Options.ShadowMap.UseFrustumIntersect &&
                recLayer.ContentVersion == _recLayerVersion &&
                castLayer.ContentVersion == _castLayerVersion &&
                _light.ContentVersion == _lightVersion)
                return false;

            if (_updateFrame == frame)
                return false;

            //Log.Debug(this, "Rendering shadow map for light '{0}'...", _light!.Name);

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
            _renderer.State.SetCullFace(TriangleFace.Back);
            _renderer.State.EnableFeature(EnableCap.CullFace, true);

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            UpdateCamera(castLayer, recLayer);

            _oldCamera = _renderer.UpdateContext.PassCamera;
            _renderer.UpdateContext.PassCamera = _lightCamera;
            _renderer.UpdateContext.ContextVersion++;

            _recLayerVersion = recLayer.ContentVersion;
            _castLayerVersion = castLayer.ContentVersion;
            _lightVersion = _light.ContentVersion;

            return base.BeginRender(camera);
        }



        protected override void EndRender()
        {
            _renderer.UpdateContext.PassCamera = _oldCamera;
            _renderer.State.SetCullFace(TriangleFace.Back);

            if (_mode == ShadowMapMode.VSM)
            {
                _frameBuffer!.Invalidate(InvalidateFramebufferAttachment.DepthAttachment);
                var radius = _renderer.Options.ShadowMap.BlurRadius;

                if (radius > 0)
                {
                    var filter = _renderer.Feature<ITextureFilterProvider>()!;

                    filter.BlurX(_vcmMomentsTex!, _vcmTempTex!, radius, "Shadow_Blur_X", 2);
                    filter.BlurY(_vcmTempTex!, _vcmMomentsTex!, radius, "Shadow_Blur_Y", 2);
                }

                var glTex = _vcmMomentsTex!.ToGlTexture();
                glTex.GenerateMipmap();

            }


            _frameBuffer!.Unbind();

            base.EndRender();
        }

        public DirectionalLight? Light => _light;

        public Texture2D? DepthTexture => _light == null ? null : (_mode == ShadowMapMode.VSM ? _vcmMomentsTex : _depthTexture);

        public Camera LightCamera => _lightCamera;

        public bool UseShadowSampler { get; set; }

        ShadowMapOptions IShadowMapProvider.Options => _renderer.Options.ShadowMap;

        Texture2D? IShadowMapProvider.ShadowMap => IsEnabled ? DepthTexture : null;
    }
}
