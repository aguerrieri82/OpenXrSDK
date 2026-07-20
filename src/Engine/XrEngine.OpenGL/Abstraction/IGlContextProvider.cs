namespace XrEngine.OpenGL
{
    public interface IGlContextProvider
    {
        IGlContext CreateShared();

        IGlContext? Current { get; }
    }
}
