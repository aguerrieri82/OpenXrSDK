namespace XrEngine.OpenGL
{
    public interface IGlRenderPass : IDisposable
    {
        void Configure(RenderContext ctx);

        void Render(RenderContext ctx);

        bool IsEnabled { get; set; }
    }
}
