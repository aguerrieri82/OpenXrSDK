namespace OpenXr.Framework
{
    public interface IXrThread
    {
        Task<T> ExecuteAsync<T>(Func<T> action);

        Task<T> ExecuteAsync<T>(Func<Task<T>> action);

        void Post(Action action);
    }

    public static class XrThreadExtensions
    {
        public static async Task ExecuteAsync(this IXrThread thread, Action action)
        {
            await thread.ExecuteAsync(() =>
            {
                action();
                return true;
            });
        }

    }
}
