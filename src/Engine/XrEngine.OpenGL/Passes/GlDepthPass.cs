#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlDepthPass : GlBaseSingleMaterialPass
    {
        public GlDepthPass(OpenGLRender renderer)
            : base(renderer)
        {
            UseOcclusion = true;
        }

        protected override bool BeginRender(Camera camera)
        {
            _renderer.RenderTarget!.Begin(camera, _renderer.RenderView.Size);
            _renderer.State.SetView(_renderer.RenderView);

            _renderer.State.SetWriteDepth(true);
            _gl.Clear(ClearBufferMask.DepthBufferBit);
            _gl.DepthFunc(DepthFunction.Less);
            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
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

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main).Take(1);
        }

        protected override void Draw(DrawContent draw)
        {
            if (UseOcclusion)
            {
                draw.Query ??= draw.Object!.GetOrCreateProp(OpenGLRender.Props.GlQuery, () => new GlQuery(_gl));
                draw.Query!.Begin(QueryTarget.AnySamplesPassed);
                draw.Draw!();
                draw.Query.End();
            }
            else
                draw.Draw!();
        }

        public bool UseOcclusion { get; set; }
    }
}
