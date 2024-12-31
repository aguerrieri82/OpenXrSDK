namespace XrEngine.OpenGL
{
    public interface IGlRenderPass : IDisposable, IRenderPass
    {
        void Configure(RenderContext ctx);

        void Render(RenderContext ctx);

        bool IsEnabled { get; set; }
    }
}
