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
        private PlanarReflection? _reflection;
        private int _boundEye;
        private Camera? _oldCamera;
        private IGlRenderAttachment? _glDepthBuffer;
        private readonly IGlRenderTarget _renderTarget;
        private bool _isTargetInit;
        private ImageLight? _imageLight;
        private Matrix3x3 _oldImageLightTransform;

        public GlFullReflectionTargetPass(OpenGLRender renderer, bool useMultiviewTarget)
            : base(renderer)
        {
            PbrV2Material.ForceIblTransform = true;

            if (PlanarReflection.IsMultiView && useMultiviewTarget)
                _renderTarget = new GlMultiViewRenderTarget(_gl);
            else
                _renderTarget = new GlTextureRenderTarget(_gl);

            if (!PlanarReflection.IsMultiView)
                _glDepthBuffer = new GlRenderBuffer(_gl);
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override bool CanDraw(DrawContent draw)
        {
            Debug.Assert(_reflection != null);

            if (draw.Object == _reflection.Host)
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
            if (camera.Scene == null)
                return false;

            _oldCamera = _renderer.UpdateContext.Camera!;

            _reflection?.Update(_oldCamera, _boundEye);

            if (_reflection?.Texture == null)
                return false;

            if (!_reflection.Host!.IsVisible || !_reflection.Host.WorldBounds.IntersectFrustum(_renderer.UpdateContext.FrustumPlanes))
                return false;
           
            if (_glDepthBuffer == null || _glDepthBuffer.Width != _reflection.Texture.Width || _glDepthBuffer.Height != _reflection.Texture.Height)
            {
                if (_renderTarget is GlMultiViewRenderTarget || PlanarReflection.IsMultiView)
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

                _isTargetInit = false;
            }

            if (!_isTargetInit || _boundEye != -1)
            { 
                if (_renderTarget is GlMultiViewRenderTarget mv)
                    mv.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), (GlTexture)_glDepthBuffer, 1);

                else if (_renderTarget is GlTextureRenderTarget tex)
                {
                    if (_boundEye != -1)
                        tex.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), (uint)_boundEye, (GlTexture)_glDepthBuffer, (uint)_boundEye, 1);
                    else
                        tex.FrameBuffer.Configure(_reflection.Texture.ToGlTexture(), _glDepthBuffer, 1);
                }

                _isTargetInit = true;
            }

            _renderer.UpdateContext.Camera = _reflection.ReflectionCamera;

            _renderTarget.Begin(_reflection.ReflectionCamera, new Size2I(_reflection.Texture.Width, _reflection.Texture.Height));

            _renderer.State.SetWriteColor(true);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetClearDepth(1.0f);
            _renderer.State.SetView(new Rect2I(0, 0, _reflection.Texture.Width, _reflection.Texture.Height));

            _gl.Clear((uint)(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit));

            if (_reflection.AdjustIbl)
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
                    //_imageLight.NotifyChanged(ObjectChangeType.Render);
                }
            }
            else
                _imageLight = null;

            return true;
        }

        protected override void EndRender()
        {
            _renderTarget.End(true);

            _renderer.UpdateContext.Camera = _oldCamera;

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
            _glDepthBuffer?.Dispose();
            _renderTarget.Dispose();
            base.Dispose();
        }

        public void SetOptions(ReflectionTarget options)
        {
            _reflection = options.PlanarReflection;
            _boundEye = options.BoundEye;
        }
    }
}
