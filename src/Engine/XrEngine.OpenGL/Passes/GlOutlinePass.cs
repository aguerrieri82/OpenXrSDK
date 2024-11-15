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
        protected Size2I _lastSize;
        protected readonly GlComputeProgram _outlineProgram;

        public GlOutlinePass(OpenGLRender renderer, int boundEye = -1)
            : base(renderer)
        {
            _passTarget = new GlRenderPassTarget(renderer.GL);
            _passTarget.BoundEye = boundEye;
            _passTarget.DepthMode = TargetDepthMode.None;
            _passTarget.AddExtra(TextureFormat.Rgba32, null, false);

            _outlineProgram = new GlComputeProgram(renderer.GL, "Image/outline.glsl", str => Embedded.GetString<Material>(str));
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

            _lastSize = camera.ViewSize;

            _passTarget.Configure(_lastSize.Width, _lastSize.Height, TextureFormat.Rgba32);
            _passTarget.RenderTarget.Begin(camera);

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


        protected override bool CanDraw(DrawContent draw)
        {
            if (!Source!.HasOutline(draw.Object!, out var color))
                return false;
            
            _programInstance!.Material.UpdateColor(color);

            return true;
        }

        protected override void EndRender()
        {
            _passTarget.RenderTarget!.End(true);

            _outlineProgram.Use();
            _outlineProgram.SetUniform("uSize", (int)_renderer.Options.Outline.Size);

            var outlineTexture = _passTarget.GetExtra(0)!;

            ProcessImage(_passTarget.ColorTexture!, outlineTexture);

            _renderer.RenderTarget!.Begin(_renderer.UpdateContext.Camera!);

            OverlayTexture(outlineTexture);
        }

        protected override IEnumerable<GlLayer> SelectLayers()
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
