namespace XrEngine.OpenGL
{
    public interface IGlRenderPass
    {
        void Render();

        bool IsEnabled { get; set; }
    }
}
