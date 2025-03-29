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
        private Camera? _oldCamera;

        public GlSimpleReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {
            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                IsMultiView = PlanarReflection.IsMultiView,
                UseMultiViewTarget = useMultiviewTarget
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

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            Debug.Assert(_reflection != null && _programInstance != null);

            if (!_reflection.PrepareMaterial(drawMaterial))
                return UpdateProgramResult.Skip;

            if (_reflection.UseClipPlane)
            {
                if (_programInstance.ExtraExtensions == null)
                {
                    _programInstance.ExtraFeatures = ["USE_CLIP_PLANE"];
                    _programInstance.ExtraExtensions = ["GL_EXT_clip_cull_distance"];
                    _programInstance.Invalidate();
                }

                var upRes = base.UpdateProgram(updateContext, drawMaterial);

                _programInstance.Program!.Use();

                _renderer.ConfigureCaps(_programInstance.Material);

                var newPlane = new Vector4(_reflection.Plane.Normal, _reflection.Plane.D);
                _programInstance.Program!.SetUniform("uClipPlane", newPlane);

                return upRes;
            }
            else
            {
                if (_programInstance.ExtraFeatures != null)
                {
                    _programInstance.ExtraFeatures = null;
                    _programInstance.ExtraExtensions = null;
                    _programInstance.Invalidate();
                }

                var upRes = base.UpdateProgram(updateContext, drawMaterial);

                _programInstance.Program!.Use();

                _renderer.ConfigureCaps(_programInstance.Material);

                return upRes;
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

        protected override bool BeginRender(Camera camera)
        {
            if (camera.Scene == null || _reflection == null)
                return false;

            if (!_reflection.Host!.IsVisible || !_reflection.Host.WorldBounds.IntersectFrustum(_renderer.UpdateContext.FrustumPlanes))
                return false;

            _oldCamera = _renderer.UpdateContext.PassCamera!;

            _reflection.Update(_oldCamera, _passTarget.BoundEye);

            _renderer.UpdateContext.PassCamera = _reflection.ReflectionCamera;

            _passTarget.Configure(_reflection.Texture!);
            _passTarget.RenderTarget!.Begin(_reflection.ReflectionCamera);

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetClearColor(_reflection.ReflectionCamera.BackgroundColor);

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _passTarget.RenderTarget!.End(true);

            _renderer.UpdateContext.PassCamera = _oldCamera;

            base.EndRender();
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
            _reflection = options.PlanarReflection;
            _passTarget.BoundEye = options.BoundEye;
            _programInstance = CreateProgram(_reflection.MaterialOverride!);
        }
    }
}
