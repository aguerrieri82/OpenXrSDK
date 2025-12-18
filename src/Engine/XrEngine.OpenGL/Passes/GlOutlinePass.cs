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
        protected readonly GlSimpleProgram _outlineProgram;
        protected Bounds2 _bounds;

        public GlOutlinePass(OpenGLRender renderer, int boundEye = -1, bool isMultiView = false)
            : base(renderer)
        {
            UseScissor = true;

            _passTarget = new GlRenderPassTarget(renderer.GL);
            _passTarget.BoundEye = boundEye;
            _passTarget.DepthMode = TargetDepthMode.None;
            _passTarget.IsMultiView = isMultiView;
            _passTarget.UseMultiViewTarget = true;

            _outlineProgram = new GlSimpleProgram(renderer.GL, "fullscreen.vert", "outline.frag", str => Embedded.GetString<Material>(str));

            if (isMultiView)
            {
                _outlineProgram.AddExtension("GL_OVR_multiview2");
                _outlineProgram.AddFeature("MULTI_VIEW");
            }

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

            if (!Source.HasOutlines())
                return false;

            _passTarget.Configure(camera.ViewSize.Width, camera.ViewSize.Height, TextureFormat.Rgba32);
            _passTarget.RenderTarget!.Begin(camera);

            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _bounds = new Bounds2();
            _bounds.Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            _bounds.Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

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

            if (_programInstance!.Material.UpdateColor(color))
                UpdateMaterial(updateContext);

            return UpdateProgramResult.Unchanged;
        }

        protected override void EndRender()
        {
            _passTarget.RenderTarget!.End(true);

            _renderer.RenderTarget!.Begin(_renderer.UpdateContext.PassCamera!);

            _outlineProgram.Use();
            _outlineProgram.SetUniform("uSize", (int)_renderer.Options.Outline.Size);
            _outlineProgram.LoadTexture(_passTarget.ColorTexture!.ToEngineTexture(), 0);

            if (UseScissor)
            {
                int padding = (int)_renderer.Options.Outline.Size + 2;
                _bounds.Min -= new Vector2(padding, padding);
                _bounds.Max += new Vector2(padding, padding);

                _renderer.State.EnableFeature(EnableCap.ScissorTest, true);

                _gl.Scissor((int)_bounds.Min.X, (int)_bounds.Min.Y, (uint)_bounds.Size.X, (uint)_bounds.Size.Y);
            }

            DrawQuad();

            if (UseScissor)
                _renderer.State.EnableFeature(EnableCap.ScissorTest, false);
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

        static bool TryGetScreenPoint(Vector3 worldPos, Camera cam, out Vector2 screenPos)
        {
            var clipPos = Vector4.Transform(new Vector4(worldPos, 1), cam.ViewProjection);

            if (clipPos.W <= 0.001f)
            {
                screenPos = Vector2.Zero;
                return false; 
            }

            var ndc = new Vector3(clipPos.X, clipPos.Y, clipPos.Z) / clipPos.W;

            var viewSize = cam.ViewSize;
            screenPos = new Vector2(
                (ndc.X + 1.0f) * 0.5f * viewSize.Width,
                (ndc.Y + 1.0f) * 0.5f * viewSize.Height 
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
                    if (!TryGetScreenPoint(corner, _renderer.UpdateContext.PassCamera!, out var screen))
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
