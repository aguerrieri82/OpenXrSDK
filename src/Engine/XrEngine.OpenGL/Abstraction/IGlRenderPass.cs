namespace XrEngine.OpenGL
{
    public interface IGlRenderPass : IDisposable
    {
        void Configure();

        void Render(RenderContext ctx);

        bool IsEnabled { get; set; }
    }
}
