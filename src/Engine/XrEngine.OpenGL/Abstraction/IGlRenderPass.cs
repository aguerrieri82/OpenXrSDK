namespace XrEngine.OpenGL
{
    public interface IGlRenderPass : IDisposable
    {
        void Render(RenderContext ctx);

        bool IsEnabled { get; set; }
    }
}
