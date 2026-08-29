
namespace XrEngine
{
    public interface IDispatcher
    {
        void Post(Action action);

        Task ExecuteAsync(Action action);

        Task<T> ExecuteAsync<T>(Func<T> action);

        Thread Thread { get; }

        DispatcherSwitch Switch => new DispatcherSwitch(this);
    }
}
