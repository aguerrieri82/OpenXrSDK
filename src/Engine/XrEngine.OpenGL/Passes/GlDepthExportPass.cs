
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{

    public class GlDepthExportPass : GlBaseSingleMaterialPass
    {
        readonly GlRenderTargetPool _pool;
        readonly ChangeTracker _tracker;
        IGlRenderTarget? _renderTarget;

        public GlDepthExportPass(OpenGLRender renderer, bool multiView)
            : base(renderer)
        {
            _pool = new GlRenderTargetPool(renderer.GL, multiView);
            _useInstanceDraw = true;
            _tracker = new();
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            if (_renderTarget == null)
                return false;

            _renderTarget!.Begin(ctx.PassCamera!);

            _renderer.State.SetWriteDepth(true);
            _gl.Clear(ClearBufferMask.DepthBufferBit);
            _gl.DepthFunc(DepthFunction.Less);

            return base.BeginRender(ctx);
        }

        protected override UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext ctx, Material drawMaterial)
        {
            if (_tracker.IsChanged(() => ctx.UseInstanceDraw))
                return UpdateProgramResult.Changed;

            return base.UpdateProgram(instance, ctx, drawMaterial);
        }

        public void Configure(uint depthTex)
        {
            _renderTarget = _pool.GetRenderTarget(0, depthTex, 1);
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _renderTarget;
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            _renderTarget!.End(false);

            base.EndRender(ctx);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new ColorMaterial
            {
                WriteColor = false,
                UseDepth = true,
                WriteDepth = true
            };
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Opaque).Take(1);
        }

        public override void Dispose()
        {
            _pool.Dispose();
            base.Dispose();
        }

    }
}
