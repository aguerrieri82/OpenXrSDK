namespace XrEngine.OpenGL
{
    public interface IGlRenderPass : IDisposable
    {
        void Render(Camera camera);

        bool IsEnabled { get; set; }
    }
}
