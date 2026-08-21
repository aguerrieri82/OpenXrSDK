namespace XrEngine.OpenGL
{
    public interface IGlDynamicRenderPass<TOptions> : IGlRenderPass
    {
        void SetOptions(TOptions options);
    }

    public abstract class GlBaseRenderPassGroup<TPass, TOptions> : IGlRenderPass where TPass : IGlDynamicRenderPass<TOptions>
    {
        protected readonly OpenGLRender _renderer;
        protected GlRenderPassFlags _flags;

        public GlBaseRenderPassGroup(OpenGLRender renderer)
        {
            _renderer = renderer;
            IsEnabled = true;
        }

        protected abstract IEnumerable<TOptions> GetPasses(GlUpdateContext ctx);

        protected abstract TPass ConfigurePass(TOptions options);

        public void Configure(GlUpdateContext ctx)
        {
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Render(GlUpdateContext ctx)
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

        public int Priority { get; set; }

        public bool IsEnabled { get; set; }

        public GlRenderPassFlags Flags => _flags;

    }
}
