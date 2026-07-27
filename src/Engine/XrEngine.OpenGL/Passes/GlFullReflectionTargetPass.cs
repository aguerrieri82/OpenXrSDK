#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlFullReflectionTargetPass : GlColorPass, IGlDynamicRenderPass<ReflectionTarget>
    {
        static readonly Bounds3 _clipSpace = new Bounds3() { Min = -Vector3.One, Max = Vector3.One };

        private readonly GlRenderPassTarget _passTarget;
        private PlanarReflection? _reflection;
        private ImageLight? _imageLight;
        private Matrix3x3 _oldImageLightTransform;
        private GlSwapTexture? _swap;
        private bool _wasSrgb;

        public GlFullReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {
            PbrMaterial.ForceIblTransform = true;

            _flags = GlRenderPassFlags.CustomCamera;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                IsMultiView = PlanarReflection.IsMultiView,
                UseMultiViewTarget = useMultiviewTarget,
                Name = "Full Reflection"
            };

        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            Debug.Assert(_reflection != null);

            if (draw.Object == _reflection.Host)
                return false;

            if (draw.ProgramInstance!.Material.Shader!.IsEffect)
                return false;

            var target = draw.Object?.Components<PlanarReflectionTarget>().FirstOrDefault();
            if (target?.IncludeReflection != null && !target.IncludeReflection(_reflection))
                return false;

            return draw.Object!.IsVisible;
        }

        protected override bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst, bool forceSync = false)
        {

            if (!_reflection!.UseClipPlane)
                return base.UpdateProgram(updateContext, progInst);

            if (progInst.ExtraExtensions == null)
            {
                progInst.ExtraFeatures = ["USE_CLIP_PLANE"];
                progInst.ExtraExtensions = ["GL_EXT_clip_cull_distance"];
                progInst.Invalidate();
            }

            var upRes = base.UpdateProgram(updateContext, progInst, forceSync);

            var newPlane = new Vector4(_reflection.Plane.Normal, _reflection.Plane.D);

            progInst.Program!.Use();
            progInst.Program!.SetUniform("uClipPlane", newPlane);

            return upRes;
        }

        protected override void ConfigureCaps(ShaderMaterial material)
        {
            base.ConfigureCaps(material);
            _renderer.State.EnableFeature(EnableCap.ClipDistance0, true);
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            var mainCamera = ctx.MainCamera!;

            if (ctx.Scene == null || _reflection == null || mainCamera.ViewSize.Width == 0 | mainCamera.ViewSize.Height == 0)
                return false;

            if (!_reflection.Host!.IsVisible)
                return false;

            _reflection.Update(mainCamera, _passTarget.BoundEye);

            if (_swap != null && _swap.Main?.Handle == 0)
            {
                _swap.Dispose();
                _swap = null;
            }

            Debug.Assert(_reflection.Texture != null);

            _swap ??= new GlSwapTexture();

            _swap.Configure(_reflection.Texture.ToGlTexture());

            var clipSize = _reflection.ClipBounds.Size.ToVector2() *
                           _reflection.ReflectionCamera.ViewSize.ToVector2() / 2;

            if (Math.Max(clipSize.X, clipSize.Y) < 20)
                return false;

            if (!_reflection.ClipBounds.Intersects(_clipSpace))
                return false;

            ctx.PassCamera = _reflection.ReflectionCamera;
            ctx.ContextVersion++;

            _passTarget.Configure(_swap.Active!);

            _passTarget.RenderTarget!.ShadingRate = _reflection.ShadingRate;

            _passTarget.RenderTarget.Begin(_reflection.ReflectionCamera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(_reflection.ReflectionCamera.BackgroundColor);

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            ProcessImageLight(ctx);

            _wasSrgb = ctx.IsSrgbTarget;

            ctx.IsSrgbTarget = _reflection.UseSrgb;

            if (_wasSrgb != ctx.IsSrgbTarget)
                PbrMaterial.SHADER.Invalidate();

            return true;
        }

        protected void ProcessImageLight(GlUpdateContext ctx)
        {
            if (_reflection!.AdjustIbl)
            {
                _imageLight = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();

                if (_imageLight != null)
                {
                    _oldImageLightTransform = _imageLight.LightTransform;

                    var normal = _reflection.Plane.Normal;

                    float nx = normal.X, ny = normal.Y, nz = normal.Z;

                    var refMatrix = new Matrix3x3(
                        1 - 2 * nx * nx, -2 * nx * ny, -2 * nx * nz,
                        -2 * ny * nx, 1 - 2 * ny * ny, -2 * ny * nz,
                        -2 * nz * nx, -2 * nz * ny, 1 - 2 * nz * nz
                    );

                    _imageLight.LightTransform = refMatrix;
                    _imageLight.Invalidate();
                }
            }
            else
                _imageLight = null;
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            Debug.Assert(_swap?.Active != null);

            _passTarget.RenderTarget!.End(discardDepth: true);

            if (_imageLight != null)
            {
                _imageLight.LightTransform = _oldImageLightTransform;
                _imageLight.Invalidate();
            }

            _swap.Active.GenerateMipmap();

            if (_reflection!.BlurLevel > 0)
                _swap.Blur(2, _reflection!.BlurLevel);

            _reflection.Texture = (Texture2D)_swap.Active.ToEngineTexture();

            if (_wasSrgb != ctx.IsSrgbTarget)
            {
                ctx.IsSrgbTarget = _wasSrgb;
                PbrMaterial.SHADER.Invalidate();
            }
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.FullReflection).Take(1);
        }

        public override void Dispose()
        {
            _passTarget.Dispose();
            _swap?.Dispose();
            base.Dispose();
        }

        public void SetOptions(ReflectionTarget options)
        {
            _reflection = options.PlanarReflection;
            _passTarget.BoundEye = options.BoundEye;


        }
    }
}
