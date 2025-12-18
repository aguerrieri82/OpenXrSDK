namespace XrEngine
{
    public class LockDispatcher : IDispatcher
    {
        readonly object _lock;

        public LockDispatcher(object obj)
        {
            _lock = obj;
        }

        public Task ExecuteAsync(Action action)
        {
            lock (_lock)
                action();
            return Task.CompletedTask;
        }

        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            lock (_lock)
                return Task.FromResult(action());
        }

        public object Lock => _lock;
    }
}
