#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlOutlinePass : GlBaseRenderPass
    {
        private uint _fooVa;
        private GlProgramInstance? _outline;
        private GlProgramInstance? _clear;


        public GlOutlinePass(OpenGLRender renderer)
            : base(renderer)
        {
        }

        protected override bool BeginRender()
        {
            _renderer.RenderTarget!.Begin();
            return base.BeginRender();
        }

        protected override void EndRender()
        {

        }

        protected override void Initialize()
        {
            var options = _renderer.Options.Outline;

            _outline = CreateProgram(new OutlineEffect()
            {
                Color = options.Color,
                Size = options.Size,
            });

            _clear = CreateProgram(new DepthClearEffect()
            {
                StencilFunction = StencilFunction.NotEqual,
                CompareStencil = options.ActiveOutlineStencil
            });


            _fooVa = _renderer.GL.GenVertexArray();

            base.Initialize();
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Main).Take(1);
        }

        protected override void RenderLayer(GlLayer layer)
        {

            _renderer.GL.BindVertexArray(_fooVa);

            UseProgram(_clear!, true);

            _renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _renderer.RenderTarget?.CommitDepth();

            _renderer.UpdateContext.DepthMap = _renderer.GetDepth();

            UseProgram(_outline!, true);

            _renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _renderer.GL.BindVertexArray(0);

            _renderer.UpdateContext.DepthMap = null;
        }


    }
}
