namespace XrEngine.OpenGL
{
    public interface IGlRenderPass
    {
        void Render(Camera camera);

        bool IsEnabled { get; set; }
    }
}
