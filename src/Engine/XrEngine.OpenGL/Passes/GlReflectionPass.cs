#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrEngine.Services;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlReflectionPass : GlBaseSingleMaterialPass
    {
        private PlanarReflection? _reflection;
        private Scene3D? _lastScene;
        private readonly IGlRenderAttachment _glDepthBuffer;
        private Camera? _oldCamera;
        private readonly IGlRenderTarget _renderTarget;

        public GlReflectionPass(OpenGLRender renderer)
            : base(renderer)
        {

            if (PlanarReflection.IsMultiView)
            {
                _renderTarget = new GlMultiViewRenderTarget(_gl);

                _glDepthBuffer = new GlTexture(_gl)
                {
                    MinFilter = TextureMinFilter.Nearest,
                    MagFilter = TextureMagFilter.Nearest,
                    MaxLevel = 0,
                    Target = TextureTarget.Texture2DArray
                };
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

        protected override bool PrepareMaterial(Material material)
        {
            Debug.Assert(_reflection != null);
            return _reflection.PrepareMaterial(material);
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

            if (_reflection == null || camera.Scene != _lastScene)
            {
                var layer = camera.Scene.EnsureLayer<ReflectionLayer>();

                var obj = layer.Content.FirstOrDefault();
                if (obj == null)
                    return false;

                _reflection = obj.Component<PlanarReflection>();

                _programInstance = CreateProgram(_reflection.MaterialOverride);

                _lastScene = camera.Scene;

                if (PlanarReflection.IsMultiView)
                {
                    ((GlTexture)_glDepthBuffer).Update(
                         _reflection.Texture.Width,
                         _reflection.Texture.Height,
                         _reflection.Texture.Depth,
                         TextureFormat.Depth24Float);
                }
                else
                {
                    ((GlRenderBuffer)_glDepthBuffer).Update(
                         _reflection.Texture.Width,
                         _reflection.Texture.Height,
                         1,
                         InternalFormat.DepthComponent24);
                }

                if (_renderTarget is GlMultiViewRenderTarget mv)
                    mv.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), (GlTexture)_glDepthBuffer, 1);

                else if (_renderTarget is GlTextureRenderTarget tex)
                    tex.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), _glDepthBuffer, 1);

                //_envLayer = CreateEnvLayer(camera.Scene);   
            }

            _oldCamera = _renderer.UpdateContext.Camera;
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
            _glDepthBuffer.Dispose();
            _renderTarget.Dispose();
            base.Dispose();
        }

        protected override void Initialize()
        {
            //DONT CALL BASE
        }
    }
}
