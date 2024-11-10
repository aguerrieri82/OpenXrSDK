#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlFullReflectionPass : GlColorPass
    {
        private PlanarReflection? _reflection;
        private Scene3D? _lastScene;
        private Camera? _oldCamera;
        private readonly IGlRenderAttachment _glDepthBuffer;
        private readonly IGlRenderTarget _renderTarget;
        private bool _isBufferInit;

        public GlFullReflectionPass(OpenGLRender renderer)
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

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            if (_reflection != null && _reflection.Host == draw.Object)
                return false;
            return draw.Object!.IsVisible;
        }

        protected override void UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst)
        {
            var newPlane = new Vector4(_reflection!.Plane.Normal, _reflection.Plane.D);

            progInst.UpdateProgram(updateContext, ["USE_CLIP_PLANE"], ["GL_EXT_clip_cull_distance"]);
            progInst.Program!.SetUniform("uClipPlane", newPlane);
        }

        protected override void Draw(DrawContent draw)
        {
            _renderer.State.EnableFeature(EnableCap.ClipDistance0, true);
            base.Draw(draw);
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

                _lastScene = camera.Scene;
            }

            if (_glDepthBuffer.Width != _reflection.Texture.Width || _glDepthBuffer.Height != _reflection.Texture.Height)
            {
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
            }

            if (!_isBufferInit)
            { 
                if (_renderTarget is GlMultiViewRenderTarget mv)
                    mv.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), (GlTexture)_glDepthBuffer, 1);

                else if (_renderTarget is GlTextureRenderTarget tex)
                    tex.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), _glDepthBuffer, 1);

                _isBufferInit = true;
            }

            _oldCamera = _renderer.UpdateContext.Camera!;

            _reflection.Update(_oldCamera);

            _renderer.UpdateContext.Camera = _reflection.ReflectionCamera;

            _renderTarget.Begin(_reflection.ReflectionCamera, new Size2I(_reflection.Texture.Width, _reflection.Texture.Height));

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetView(new Rect2I(0, 0, _reflection.Texture.Width, _reflection.Texture.Height));

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            return true;
        }

        protected override void EndRender()
        {
            _renderTarget.End(true);

            _renderer.UpdateContext.Camera = _oldCamera;
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
    }
}
