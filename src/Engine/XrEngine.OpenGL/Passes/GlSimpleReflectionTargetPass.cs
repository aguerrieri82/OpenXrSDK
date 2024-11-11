#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Numerics;
using XrEngine.Services;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlSimpleReflectionTargetPass : GlBaseSingleMaterialPass, IGlDynamicRenderPass<ReflectionTarget>
    {
        private PlanarReflection? _reflection;
        private Camera? _oldCamera;
        private bool _isBufferInit;
        private IGlRenderAttachment? _glDepthBuffer;
        private readonly IGlRenderTarget _renderTarget;
        private int _boundEye;

        public GlSimpleReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {

            if (PlanarReflection.IsMultiView && useMultiviewTarget)
            {
                _renderTarget = new GlMultiViewRenderTarget(_gl);
            }
            else
            {
                _renderTarget = new GlTextureRenderTarget(_gl);
                _glDepthBuffer = new GlRenderBuffer(_gl);
            }
        }

        protected GlLayer CreateEnvLayer(Scene3D scene)
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
            var glLayer = new GlLayer(_renderer, scene, GlLayerType.Custom, layer);
            glLayer.Update();
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
            return _renderTarget;
        }

        protected override bool BeginRender(Camera camera)
        {
            if (camera.Scene == null)
                return false;

            if (_reflection?.Texture == null)
                return false;

            if (_glDepthBuffer == null || _glDepthBuffer.Width != _reflection.Texture.Width || _glDepthBuffer.Height != _reflection.Texture.Height)
            {
                if (_renderTarget is GlMultiViewRenderTarget)
                {
                    _glDepthBuffer?.Dispose();

                    _glDepthBuffer = new GlTexture(_gl)
                    {
                        MinFilter = TextureMinFilter.Nearest,
                        MagFilter = TextureMagFilter.Nearest,
                        MaxLevel = 0,
                        Target = TextureTarget.Texture2DArray
                    };

                    ((GlTexture)_glDepthBuffer).Update(_reflection.Texture.Depth, new TextureData
                    {
                        Width = _reflection.Texture.Width,
                        Height = _reflection.Texture.Height,
                        Format = TextureFormat.Depth24Float
                    });
                }
                else
                {
                    ((GlRenderBuffer)_glDepthBuffer!).Update(
                         _reflection.Texture.Width,
                         _reflection.Texture.Height,
                         1,
                         InternalFormat.DepthComponent24);
                }

                _isBufferInit = false;
            }

            if (!_isBufferInit)
            {
                if (_renderTarget is GlMultiViewRenderTarget mv)
                    mv.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), (GlTexture)_glDepthBuffer, 1);

                else if (_renderTarget is GlTextureRenderTarget tex)
                    tex.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), _glDepthBuffer, 1);

                _isBufferInit = true;
            }

            _oldCamera = _renderer.UpdateContext.Camera;
            _reflection.Update(_oldCamera!, _boundEye);

            _renderer.UpdateContext.Camera = _reflection.ReflectionCamera;

            _renderTarget.Begin(_reflection.ReflectionCamera, new Size2I(_reflection.Texture.Width, _reflection.Texture.Height));

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetView(new Rect2I(0, 0, _reflection.Texture.Width, _reflection.Texture.Height));

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            _renderTarget.End(true);

            _renderer.UpdateContext.Camera = _oldCamera;

            base.EndRender();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main).Take(1);
        }

        public override void Dispose()
        {
            _glDepthBuffer?.Dispose();
            _renderTarget.Dispose();
            base.Dispose();
        }

        protected override void Initialize()
        {
            //DONT CALL BASE
        }

        public void SetOptions(ReflectionTarget options)
        {
            _reflection = options.PlanarReflection;
            _boundEye = options.BoundEye;
            _programInstance = CreateProgram(_reflection.MaterialOverride!);
        }
    }
}
