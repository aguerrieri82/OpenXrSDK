namespace OpenXr.Engine.OpenGL
{
    public interface IGlRenderTarget : IDisposable
    {
        void Begin();

        void End();
    }
}
