namespace XrEngine
{
    public class AsyncTaskDispatcher
    {
        readonly Dictionary<string, Task> _tasks = [];
        readonly SemaphoreSlim _dictMutex = new(1, 1);
        readonly SemaphoreSlim _queueLimit;
        readonly ThreadPriority _priority;

        public AsyncTaskDispatcher(int maxParallelism, ThreadPriority priority)
        {
            _queueLimit = new(maxParallelism, maxParallelism);
            _priority = priority;
        }

        public async Task<T> ExecuteAsync<T>(Func<T> action, string taskId)
        {
            Task task;

            await _dictMutex.WaitAsync();

            try
            {
                if (!_tasks.TryGetValue(taskId, out task!))
                {
                    task = Task.Run(async () =>
                    {
                        await _queueLimit.WaitAsync();
                        try
                        {
                            using var th = ThreadPriorityManager.Switch(_priority);

                            return action();
                        }
                        finally
                        {
                            _queueLimit.Release();
                        }
                    });

                    _tasks[taskId] = task;
                }
            }
            finally
            {
                _dictMutex.Release();
            }

            try
            {
                return await (Task<T>)task;
            }
            finally
            {
                await _dictMutex.WaitAsync();
                try
                {
                    _tasks.Remove(taskId);
                }
                finally
                {
                    _dictMutex.Release();
                }
            }
        }
    }
}
