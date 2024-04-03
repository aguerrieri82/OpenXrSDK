
namespace XrEngine
{
    public interface IDispatcher
    {
        Task ExecuteAsync(Action action);

        Task<T> ExecuteAsync<T>(Func<T> action);
    }

}
