#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

using XrMath;
using System.Numerics;

namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseSingleMaterialPass
    {
        protected readonly GlRenderPassTarget _passTarget;
        protected readonly GlRenderPassTarget? _tempTarget;
        protected readonly GlSimpleProgram _outlineProgram;
        protected Bounds2 _bounds;
        protected Size2I _lastCameraSize;
        protected Size2I _frameSize;
        protected float _downsampleFactor;
        protected readonly bool _isDownsample;

        public GlOutlinePass(OpenGLRender renderer, int boundEye = -1, bool isMultiView = false)
            : base(renderer)
        {
            UseScissor = true;
            
            _downsampleFactor = _renderer.Options.Outline.DownsampleFactor;
      
            _isDownsample = _downsampleFactor > 1f;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                BoundEye = boundEye,
                DepthMode = TargetDepthMode.None,
                IsMultiView = isMultiView,
                UseMultiViewTarget = true
            };

            /*
            if (_isDownsample)
                _passTarget.AddExtra(TextureFormat.Rgba32, FramebufferAttachment.ColorAttachment1, true);
            */
            if (_isDownsample)
            {
                _tempTarget = new GlRenderPassTarget(renderer.GL)
                {
                    BoundEye = boundEye,
                    DepthMode = TargetDepthMode.None,
                    IsMultiView = isMultiView,
                    UseMultiViewTarget = true
                };
            }

            _outlineProgram = new GlSimpleProgram(renderer.GL, "fullscreen.vert", "outline.frag", str => Embedded.GetString<Material>(str));

            if (isMultiView)
            {
                _outlineProgram.AddExtension("GL_OVR_multiview2");
                _outlineProgram.AddFeature("MULTI_VIEW");
            }

            //_outlineProgram.AddFeature($"FRAG_LOCATON {(_isDownsample ? 1 : 0)}");

            _outlineProgram.AddFeature($"FRAG_LOCATON 0");
            _outlineProgram.AddFeature($"OUTLINE_SIZE {_renderer.Options.Outline.Size}");

            _outlineProgram.Build();
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected override bool BeginRender(Camera camera)
        {
            if (Source == null)
            {
                if (!Context.TryRequire<IOutlineSource>(out var source))
                    return false;
                Source = source;
            }

            if (_renderer.RenderTarget is not GlMultiViewRenderTarget && _passTarget.IsMultiView)
                return false;

            if (!Source.HasOutlines())
                return false;

            _lastCameraSize = camera.ViewSize;

            _frameSize = new Size2I((uint)(camera.ViewSize.Width / _downsampleFactor), (uint)(camera.ViewSize.Height / _downsampleFactor));

            _passTarget.Configure(_frameSize.Width,
                                  _frameSize.Height, TextureFormat.GrayInt8);

            _passTarget.RenderTarget!.Begin(camera);
            
         
            /*
            if (_isDownsample)
                _passTarget.FrameBuffer!.SetDrawBuffers(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);
            */

            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _bounds = new Bounds2
            {
                Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity),
                Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            };

            return base.BeginRender(camera);
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            _programInstance!.Material.DoubleSided = drawMaterial.DoubleSided;

            return base.UpdateProgram(updateContext, drawMaterial);
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Object3D model)
        {
            if (!Source!.HasOutline(model, out var color))
                return UpdateProgramResult.Skip;

            if (_programInstance!.Material.UpdateColor(Color.White))
                UpdateMaterial(updateContext);

            return UpdateProgramResult.Unchanged;
        }

        protected override void EndRender()
        {
            var camera = _renderer.UpdateContext.PassCamera!;

            _passTarget.RenderTarget!.End(false);

            if (!_isDownsample)
            {
                _renderer.RenderTarget!.Begin(camera);
            }
            else
            {
                _tempTarget!.Configure(_frameSize.Width,
                                       _frameSize.Height, TextureFormat.Rgba32);

                _tempTarget!.RenderTarget!.Begin(camera);
                _gl.Clear(ClearBufferMask.ColorBufferBit);
            }
               
            _outlineProgram.Use();

            _outlineProgram.SetUniform("uColor", _renderer.Options.Outline.Color);
            _outlineProgram.LoadTexture(_passTarget.ColorTexture!.ToEngineTexture(), 0);

            if (UseScissor)
            {
                var padding = (int)_renderer.Options.Outline.Size + 2;
                _bounds.Min -= new Vector2(padding, padding);
                _bounds.Max += new Vector2(padding, padding);

                _renderer.State.EnableFeature(EnableCap.ScissorTest, true);

                _gl.Scissor((int)_bounds.Min.X, (int)_bounds.Min.Y, (uint)_bounds.Size.X, (uint)_bounds.Size.Y);
            }

            DrawQuad();


            if (UseScissor)
                _renderer.State.EnableFeature(EnableCap.ScissorTest, false);

            if (_isDownsample)
            {
                _tempTarget!.RenderTarget!.End(false);

                camera.ViewSize = _lastCameraSize;

                _renderer.RenderTarget!.Begin(camera);

                OverlayTexture(_tempTarget.ColorTexture!, _passTarget.IsMultiView);
            }

        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers
                .Where(a =>
                (a.SceneLayer is DetachedLayer det) &&
                (det.Usage & DetachedLayerUsage.Outline) != 0);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new ColorMaterial()
            {
                Color = Color.White,
                WriteDepth = false,
                UseDepth = false,
            };
        }

        public override void Dispose()
        {
            _outlineProgram.Dispose();
            _passTarget.Dispose();
            base.Dispose();
        }

        bool TryGetScreenPoint(Vector3 worldPos, out Vector2 screenPos)
        {
            var camera = _renderer.UpdateContext.PassCamera;

            var viewProj = camera.Eyes != null ? camera.Eyes[Math.Max(camera.ActiveEye, 0)].ViewProj : camera.ViewProjection;

            var clipPos = Vector4.Transform(new Vector4(worldPos, 1), viewProj);

            if (clipPos.W <= 0.001f)
            {
                screenPos = Vector2.Zero;
                return false;
            }

            var ndc = new Vector3(clipPos.X, clipPos.Y, clipPos.Z) / clipPos.W;

            screenPos = new Vector2(
                (ndc.X + 1.0f) * 0.5f * _frameSize.Width,
                (ndc.Y + 1.0f) * 0.5f * _frameSize.Height
            );

            return true;
        }

        protected override void Draw(DrawContent draw)
        {
            if (UseScissor)
            {
                var bound = draw.Object!.WorldBounds;

                var objectClipping = false;

                foreach (var corner in bound.Points)
                {
                    if (!TryGetScreenPoint(corner, out var screen))
                    {
                        objectClipping = true;
                        break;
                    }

                    _bounds.Min = Vector2.Min(_bounds.Min, screen);
                    _bounds.Max = Vector2.Max(_bounds.Max, screen);
                }

                if (objectClipping)
                {
                    var size = _renderer.UpdateContext.PassCamera!.ViewSize;
                    _bounds.Min = Vector2.Zero;
                    _bounds.Max = new Vector2(size.Width, size.Height);
                }
            }

            base.Draw(draw);
        }

        public IOutlineSource? Source { get; set; }

        public GlRenderPassTarget PassTarget => _passTarget;

        public bool UseScissor { get; set; }



    }
}
