#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;
using System.Numerics;
using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseSingleMaterialPass
    {
        protected readonly GlRenderPassTarget _passTarget;
        protected readonly GlRenderPassTarget? _tempTarget;
        protected readonly OutlineEffect _outlineMat;
        protected Bounds2 _bounds;
        protected Size2I _frameSize;
        private IGlCompositor? _compositor;
        protected readonly float _downsampleFactor;
        protected readonly bool _isDownsample;

        public GlOutlinePass(OpenGLRender renderer, int boundEye = -1, bool isMultiView = false)
            : base(renderer)
        {

            _downsampleFactor = _renderer.Options.Outline.DownsampleFactor;

            _isDownsample = _downsampleFactor > 1f;

            _flags = GlRenderPassFlags.CustomCamera;

            _passTarget = new GlRenderPassTarget(renderer.GL)
            {
                BoundEye = boundEye,
                DepthMode = TargetDepthMode.None,
                IsMultiView = isMultiView,
                UseMultiViewTarget = true,
                ColorFormat = TextureFormat.Gray8,
                Name = "Outline"
            };

            _tempTarget = new GlRenderPassTarget(renderer.GL)
            {
                BoundEye = boundEye,
                DepthMode = TargetDepthMode.None,
                IsMultiView = isMultiView,
                UseMultiViewTarget = true,
                ColorFormat = TextureFormat.Rgba8,
                Id = "temp",
                Name = "Outline (Temp)"
            };

            _outlineMat = new OutlineEffect()
            {
                IsMultiView = isMultiView,
                OutlineSize = _renderer.Options.Outline.Size,
                Color = _renderer.Options.Outline.Color,
            };
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected override bool BeginRender(GlUpdateContext ctx)
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

            var camera = ctx.PassCamera!;

            _frameSize = new Size2I((uint)(camera.ViewSize.Width / _downsampleFactor), (uint)(camera.ViewSize.Height / _downsampleFactor));

            _passTarget.Configure(_frameSize.Width, _frameSize.Height);

            _passTarget.RenderTarget!.Begin(camera);

            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(false);
            _renderer.State.SetWriteColor(true);
            _renderer.State.Commit();

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _bounds = new Bounds2
            {
                Min = new Vector2(float.PositiveInfinity, float.PositiveInfinity),
                Max = new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            };

            return base.BeginRender(ctx);
        }

        protected override UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext updateContext, Material drawMaterial)
        {
            var effect = instance.Material;

            var hasSkin = drawMaterial is ShaderMaterial mat && mat.UseSkin;
            var isChanged = hasSkin != effect.UseSkin;

            effect.DoubleSided = drawMaterial.DoubleSided;
            effect.UseSkin = hasSkin;

            var result = base.UpdateProgram(instance, updateContext, drawMaterial);

            if (result == UpdateProgramResult.Skip)
                return UpdateProgramResult.Skip;

            return isChanged ? UpdateProgramResult.Changed : result;
        }

        protected override UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext updateContext, Object3D model)
        {
            if (!Source!.HasOutline(model, out var color))
                return UpdateProgramResult.Skip;

            if (instance!.Material.UpdateColor(Color.White))
                UpdateMaterial(instance, updateContext);

            return UpdateProgramResult.Unchanged;
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            var camera = ctx.PassCamera!;

            _passTarget.RenderTarget!.End(discardDepth: true);

            //Process Mask

            _tempTarget!.Configure(_frameSize.Width, _frameSize.Height);

            _tempTarget!.RenderTarget!.Begin(camera);

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            var padding = (int)_renderer.Options.Outline.Size + 2;

            _renderer.SetScissor(_bounds, padding);

            _outlineMat.Texture = _passTarget.Color!.ToEngineTexture();

            UseEffect(_outlineMat);

            DrawQuad();

            _tempTarget!.RenderTarget!.End(discardDepth: true);

            //Composition

            _compositor ??= _renderer.Feature<IGlCompositor>();

            Debug.Assert(_compositor != null);

            var region = _bounds.Scale(_downsampleFactor);

            _compositor.AppendTexture(_tempTarget.Color!, region);

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
                Skin = SkinMode.Dynamic
            };
        }

        public override void Dispose()
        {
            _outlineMat.Dispose();
            _passTarget.Dispose();
            _tempTarget?.Dispose();
            base.Dispose();
        }


        protected override void Draw(DrawContent draw)
        {
            var camera = _renderer.UpdateContext.PassCamera!;

            var bound = draw.Object!.WorldBounds;

            var objectClipping = false;

            var eyes = _passTarget.IsMultiView ? 2 : 1;

            foreach (var corner in bound.Points)
            {
                for (var eye = 0; eye < eyes; eye++)
                {
                    if (!camera.TryWorldToScreen(corner, eye, _renderer.Features.IsAngle, out var screen))
                    {
                        objectClipping = true;
                        break;
                    }

                    _bounds.Min = Vector2.Min(_bounds.Min, screen);
                    _bounds.Max = Vector2.Max(_bounds.Max, screen);
                }
            }

            if (objectClipping)
            {
                _bounds.Min = Vector2.Zero;
                _bounds.Max = new Vector2(_frameSize.Width, _frameSize.Height);
            }

            base.Draw(draw);
        }

        public IOutlineSource? Source { get; set; }

        public GlRenderPassTarget PassTarget => _passTarget;

    }
}
