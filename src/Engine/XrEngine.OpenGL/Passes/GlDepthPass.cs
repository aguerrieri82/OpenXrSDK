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

        protected override bool BeginRender()
        {
            _renderer.RenderTarget!.Begin();
            _renderer.State.SetView(_renderer.RenderView);

            _renderer.State.SetWriteDepth(true);
            _renderer.GL.Clear(ClearBufferMask.DepthBufferBit);
            _renderer.GL.DepthFunc(DepthFunction.Less);
            return base.BeginRender();

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
            yield return _renderer.Layers[0];
        }

        protected override void Draw(DrawContent draw)
        {
            if (UseOcclusion)
            {
                draw.Query ??= draw.Object!.GetOrCreateProp(OpenGLRender.Props.GlQuery, () => new GlQuery(_renderer.GL));

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
