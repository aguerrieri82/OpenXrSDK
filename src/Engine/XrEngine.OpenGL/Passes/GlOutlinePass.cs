#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;


#endif

using XrMath;


namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseSingleMaterialPass
    {
        protected readonly GlRenderPassTarget _passTarget;
        protected readonly GlSimpleProgram _outlineProgram;

        public GlOutlinePass(OpenGLRender renderer, int boundEye = -1)
            : base(renderer)
        {
            _passTarget = new GlRenderPassTarget(renderer.GL);
            _passTarget.BoundEye = boundEye;
            _passTarget.DepthMode = TargetDepthMode.None;

            _outlineProgram = new GlSimpleProgram(renderer.GL, "fullscreen.vert", "outline.frag", str => Embedded.GetString<Material>(str));
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

            DrawQuad();
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

        public IOutlineSource? Source { get; set; }

    }
}
