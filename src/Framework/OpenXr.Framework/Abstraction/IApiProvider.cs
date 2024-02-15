namespace OpenXr.Framework
{
    public interface IApiProvider
    {
        T GetApi<T>() where T : class;
    }
}
