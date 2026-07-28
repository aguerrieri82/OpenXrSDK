namespace XrEngine.OpenGL
{
    [Flags]
    public enum GlRenderPassFlags
    {
        None,
        CustomCamera = 1
    }

    public interface IGlRenderPass : IDisposable, IRenderPass
    {
        void Configure(GlUpdateContext ctx);

        void Render(GlUpdateContext ctx);

        bool IsEnabled { get; set; }

        GlRenderPassFlags Flags { get; }

        int Priority { get; }
    }
}
