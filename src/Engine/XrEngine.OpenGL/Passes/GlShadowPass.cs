
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
        private ShadowMapMode _mode;
        private bool _useShadowSampler;

        private readonly OrtoCamera _lightCamera;

        public GlShadowPass(OpenGLRender renderer)
            : base(renderer)
        {
            _mode = renderer.Options.ShadowMap.Mode;

            _useShadowSampler = renderer.Options.ShadowMap.UseShadowSampler && _mode != ShadowMapMode.VSM;

            _lightCamera = new OrtoCamera
            {
                Name = "Shadow"
            };

            var scaleDepth = _useShadowSampler && _mode != ShadowMapMode.VSM ? ScaleFilter.Linear : ScaleFilter.Nearest;   

            _depthTexture = new Texture2D
            {
                BorderColor = Color.White,
                WrapT = WrapMode.ClampToBorder,
                WrapS = WrapMode.ClampToBorder,
                Width = _renderer.Options.ShadowMap.Size,
                Height = _renderer.Options.ShadowMap.Size,
                Format = TextureFormat.Depth24,
                MinFilter = scaleDepth,
                MagFilter = scaleDepth,
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
                    Format = TextureFormat.RgbaFloat16,
                    MinFilter = ScaleFilter.Linear,
                    MagFilter = ScaleFilter.Linear,
                    MaxAnisotropy = 16.0f,
                    MipLevelCount = 1,
                    Name = "Moments"
                };


                _vcmTempTex = new Texture2D
                {
                    BorderColor = Color.White,
                    WrapT = WrapMode.ClampToBorder,
                    WrapS = WrapMode.ClampToBorder,
                    Width = _renderer.Options.ShadowMap.Size,
                    Height = _renderer.Options.ShadowMap.Size,
                    Format = TextureFormat.RgbaFloat16,
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

            glDeptTex.Bind();

            if (_useShadowSampler)
            {
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
            }
            else
            {
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            }

            glDeptTex.Unbind();

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

        protected bool UpdateCamera(CastShadowsLayer castLayer, ReceiveShadowsLayer recvLayer)
        {
            Debug.Assert(_light != null);

            _lightCamera.CreateViewFromDirection(_light.Direction, Vector3.UnitY);

            var options = _renderer.Options.ShadowMap;

            var receiveBoundsLight = recvLayer.WorldBounds.Points.ComputeBounds(_lightCamera.View);
            var castBoundsLight = castLayer.WorldBounds.Points.ComputeBounds(_lightCamera.View);

            Bounds3 receiverBoundsLight;

            if (options.UseFrustumIntersect)
            {
                var frustumPoints = _renderer.UpdateContext.PassCamera!.FrustumPoints();
                var frustumLightBounds = frustumPoints.ComputeBounds(_lightCamera.View);

                if (!frustumLightBounds.Intersects(receiveBoundsLight, out receiverBoundsLight))
                    return false;
            }
            else
            {
                receiverBoundsLight = receiveBoundsLight;
            }

            receiverBoundsLight.Min -= options.Expand;
            receiverBoundsLight.Max += options.Expand;

            var finalBounds = receiverBoundsLight;

            // XY: fit shadow map to visible receivers.
            finalBounds.Min.X = receiverBoundsLight.Min.X;
            finalBounds.Max.X = receiverBoundsLight.Max.X;
            finalBounds.Min.Y = receiverBoundsLight.Min.Y;
            finalBounds.Max.Y = receiverBoundsLight.Max.Y;

            // Z: include casters so off-screen casters can still project shadows.
            finalBounds.Min.Z = Math.Min(castBoundsLight.Min.Z, receiverBoundsLight.Min.Z);
            finalBounds.Max.Z = Math.Max(castBoundsLight.Max.Z, receiverBoundsLight.Max.Z);

            var zNear = Math.Max(0.05f, -finalBounds.Max.Z);
            var zFar = Math.Max(zNear + 0.01f, -finalBounds.Min.Z);

            _lightCamera.Near = Math.Max(0.05f, zNear - 1.0f);
            _lightCamera.Far = zFar + 1.0f;

            _lightCamera.SetViewArea(
                finalBounds.Min.X,
                finalBounds.Max.X,
                finalBounds.Min.Y,
                finalBounds.Max.Y);

            return true;
        }

        protected override bool BeginRender(Camera camera)
        {
            var curMode = _renderer.Options.ShadowMap.Mode;
            if (curMode != _mode)
            {
                _mode = curMode;
                Initialize();
            }


            //Debug.Assert(camera.Scene != null);
            var shadowRenderLayer = SelectLayers().First();
            var scene = shadowRenderLayer.Scene!;
            var recLayer = scene.EnsureLayer<ReceiveShadowsLayer>();
            var castLayer = scene.EnsureLayer<CastShadowsLayer>();
            var frame = scene.App!.RenderContext.Frame;

            if (_light == null)
                return false;

            if (recLayer.Content.Count == 0)
                return false;

            var curLightVers = _light.ContentVersion + _light.Version;

            if (!_renderer.Options.ShadowMap.UseFrustumIntersect &&
                recLayer.ContentVersion == _recLayerVersion &&
                castLayer.ContentVersion == _castLayerVersion &&
                curLightVers == _lightVersion)
                return false;

            if (_updateFrame == frame)
                return false;

            if (!UpdateCamera(castLayer, recLayer))
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
                
            _oldCamera = _renderer.UpdateContext.PassCamera;
            _renderer.UpdateContext.PassCamera = _lightCamera;
            _renderer.UpdateContext.ContextVersion++;

            _recLayerVersion = recLayer.ContentVersion;
            _castLayerVersion = castLayer.ContentVersion;
            _lightVersion = curLightVers;

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

                /*
                var glTex = _vcmMomentsTex!.ToGlTexture();
                glTex.GenerateMipmap();
                */
            }

            _frameBuffer!.Unbind();

            base.EndRender();
        }

        public DirectionalLight? Light => _light;

        public Texture2D? DepthTexture => _light == null ? null : (_mode == ShadowMapMode.VSM ? _vcmMomentsTex : _depthTexture);

        public Camera LightCamera => _lightCamera;

   

        ShadowMapOptions IShadowMapProvider.Options => _renderer.Options.ShadowMap;

        Texture2D? IShadowMapProvider.ShadowMap => IsEnabled ? DepthTexture : null;
    }
}
