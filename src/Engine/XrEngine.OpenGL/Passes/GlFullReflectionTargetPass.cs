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
        private Camera? _oldCamera;
        private ImageLight? _imageLight;
        private Matrix3x3 _oldImageLightTransform;


        public GlFullReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {
            PbrV2Material.ForceIblTransform = true;
            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                IsMultiView = PlanarReflection.IsMultiView,
                UseMultiViewTarget = useMultiviewTarget
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

        protected override bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst)
        {
            if (!_reflection!.UseClipPlane)
                return base.UpdateProgram(updateContext, progInst);

            if (progInst.ExtraExtensions == null)
            {
                progInst.ExtraFeatures = ["USE_CLIP_PLANE"];
                progInst.ExtraExtensions = ["GL_EXT_clip_cull_distance"];
                progInst.Invalidate();
            }

            var upRes = base.UpdateProgram(updateContext, progInst);

            var newPlane = new Vector4(_reflection.Plane.Normal, _reflection.Plane.D);

            progInst.Program!.Use();
            progInst.Program!.SetUniform("uClipPlane", newPlane);

            return upRes;
        }

        protected override void Draw(DrawContent draw)
        {
            _renderer.State.EnableFeature(EnableCap.ClipDistance0, true);
            base.Draw(draw);
        }

        protected override bool BeginRender(Camera camera)
        {
            if (camera.Scene == null || _reflection == null)
                return false;

            if (!_reflection.Host!.IsVisible)
                return false;

            _reflection.Update(camera, _passTarget.BoundEye);

            var clipSize = _reflection.ClipBounds.Size.ToVector2() *
                           _reflection.ReflectionCamera.ViewSize.ToVector2() / 2;

            if (Math.Max(clipSize.X, clipSize.Y) < 20)
                return false;

            if (!_reflection.ClipBounds.Intersects(_clipSpace))
                return false;

            _oldCamera = camera;

            _renderer.UpdateContext.PassCamera = _reflection.ReflectionCamera;

            _passTarget.Configure(_reflection.Texture!);
            _passTarget.RenderTarget.Begin(_reflection.ReflectionCamera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(_reflection.ReflectionCamera.BackgroundColor);

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            ProcessImageLight();

            return true;
        }

        protected void ProcessImageLight()
        {
            if (_reflection!.AdjustIbl)
            {
                _imageLight = _renderer.UpdateContext.Lights?.OfType<ImageLight>().FirstOrDefault();

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
                }
            }
            else
                _imageLight = null;
        }

        protected override void EndRender()
        {
            _passTarget.RenderTarget!.End(true);

            _renderer.UpdateContext.PassCamera = _oldCamera;

            if (_imageLight != null)
            {
                _imageLight.LightTransform = _oldImageLightTransform;
                //_imageLight.NotifyChanged(ObjectChangeType.Render);
            }
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.FullReflection).Take(1);
        }

        public override void Dispose()
        {
            _passTarget.Dispose();
            base.Dispose();
        }

        public void SetOptions(ReflectionTarget options)
        {
            _reflection = options.PlanarReflection;
            _passTarget.BoundEye = options.BoundEye;
        }
    }
}
