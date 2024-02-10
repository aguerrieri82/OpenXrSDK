namespace OpenXr.Framework
{
    public enum ApiType
    {
        OpenGLES
    }

    public interface IApiProvider
    {
        T GetApi<T>() where T : class;
    }
}
