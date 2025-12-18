namespace XrEngine.OpenGL
{
    public interface IGlDynamicRenderPass<TOptions> : IGlRenderPass
    {
        void SetOptions(TOptions options);
    }

    public abstract class GlBaseRenderPassGroup<TPass, TOptions> : IGlRenderPass where TPass : IGlDynamicRenderPass<TOptions>
    {
        protected readonly OpenGLRender _renderer;

        public GlBaseRenderPassGroup(OpenGLRender renderer)
        {
            _renderer = renderer;
            IsEnabled = true;
        }

        protected abstract IEnumerable<TOptions> GetPasses(RenderContext ctx);

        protected abstract TPass ConfigurePass(TOptions options);

        public void Configure(RenderContext ctx)
        {
        }

        public void Dispose()
        {
        }

        public void Render(RenderContext ctx)
        {
            if (!IsEnabled)
                return;

            var passOptions = GetPasses(ctx);

            foreach (var opt in passOptions)
            {
                var pass = ConfigurePass(opt);
                pass.Render(ctx);
            }
        }

        public bool IsEnabled { get; set; }

    }
}
