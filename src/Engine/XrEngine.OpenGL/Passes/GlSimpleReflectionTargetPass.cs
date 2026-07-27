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
    public class GlSimpleReflectionTargetPass : GlBaseSingleMaterialPass, IGlDynamicRenderPass<ReflectionTarget>
    {
        private readonly GlRenderPassTarget _passTarget;
        private PlanarReflection? _reflection;

        public GlSimpleReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {

            _flags = GlRenderPassFlags.CustomCamera;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                IsMultiView = PlanarReflection.IsMultiView,
                UseMultiViewTarget = useMultiviewTarget,
                Name = "Simple Reflection"
            };
        }

        protected IGlLayer CreateEnvLayer(Scene3D scene)
        {
            var layer = new DetachedLayer();

            var env = new TriangleMesh(new IsoSphere3D(2, 3), new TextureMaterial
            {
                UseDepth = true,
                WriteDepth = false,
                DoubleSided = true,
                Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/Envs/CameraEnv.jpg"),
            });

            scene.AddChild(env);

            layer.Add(env);
            var glLayer = _renderer.AddLayer(scene, GlLayerType.Custom, layer);
            glLayer.Rebuild();
            return glLayer;
        }

        protected override ShaderMaterial CreateMaterial()
        {
            throw new NotSupportedException();
        }

        protected override UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext updateContext, Material drawMaterial)
        {
            Debug.Assert(_reflection != null);

            if (!_reflection.PrepareMaterial(drawMaterial))
                return UpdateProgramResult.Skip;

            if (_reflection.UseClipPlane)
            {
                if (instance.ExtraExtensions == null)
                {
                    instance.ExtraFeatures = ["USE_CLIP_PLANE"];
                    instance.ExtraExtensions = ["GL_EXT_clip_cull_distance"];
                    instance.Invalidate();
                }

                var upRes = base.UpdateProgram(instance, updateContext, drawMaterial);

                instance.Program!.Use();

                var newPlane = new Vector4(_reflection.Plane.Normal, _reflection.Plane.D);
                instance.Program!.SetUniform("uClipPlane", newPlane);

                return UpdateProgramResult.Changed;
            }
            else
            {
                if (instance.ExtraFeatures != null)
                {
                    instance.ExtraFeatures = null;
                    instance.ExtraExtensions = null;
                    instance.Invalidate();
                }

                var upRes = base.UpdateProgram(instance, updateContext, drawMaterial);

                instance.Program!.Use();

                return UpdateProgramResult.Changed;
            }
        }

        protected override bool CanDraw(DrawContent draw)
        {
            Debug.Assert(_reflection != null);

            if (draw.Object == _reflection.Host)
                return false;

            var target = draw.Object?.Components<PlanarReflectionTarget>().FirstOrDefault();
            if (target?.IncludeReflection != null && !target.IncludeReflection(_reflection))
                return false;

            return true;
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            if (ctx.Scene == null || _reflection == null)
                return false;

            if (!_reflection.Host!.IsVisible ||
                !_reflection.Host.WorldBounds.IntersectFrustum(ctx.FrustumPlanes
                 .AsSpan(0, ctx.FrustumPlanesCount)))
            {
                return false;
            }

            _reflection.Update(ctx.MainCamera!, _passTarget.BoundEye);

            ctx.PassCamera = _reflection.ReflectionCamera;
            ctx.ContextVersion++;

            _passTarget.Configure(_reflection.Texture!);
            _passTarget.RenderTarget!.Begin(_reflection.ReflectionCamera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(_reflection.ReflectionCamera.BackgroundColor);

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return base.BeginRender(ctx);
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            _passTarget.RenderTarget!.End(discardDepth: true);

            base.EndRender(ctx);
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Opaque).Take(1);
        }

        public override void Dispose()
        {
            _passTarget.Dispose();
            base.Dispose();
        }

        protected override void Initialize()
        {
            //DONT CALL BASE
        }

        public void SetOptions(ReflectionTarget options)
        {
            Debug.Assert(!_useInstanceDraw);

            _reflection = options.PlanarReflection;
            _passTarget.BoundEye = options.BoundEye;

            _progInstBase = GetProgramInstance(_reflection.MaterialOverride!);
        }
    }
}
