using System.Collections.Concurrent;

namespace XrEngine
{
    public class QueueDispatcher : IDispatcher
    {
        public class QueueTask
        {
            public Func<object?>? Action;

            public TaskCompletionSource<object?>? Completion;
        }

        protected readonly ConcurrentQueue<QueueTask> _queue = [];
        protected bool _isProcessingQueue;
        protected readonly Thread _thread;

        public QueueDispatcher()
        {
            _thread = Thread.CurrentThread;
        }

        public async Task ExecuteAsync(Action action)
        {
            if (Thread.CurrentThread == _thread)
            {
                action();
                return;
            }

            var task = new QueueTask()
            {
                Action = () =>
                {
                    action();
                    return null;
                },
                Completion = new TaskCompletionSource<object?>()
            };

            _queue.Enqueue(task);

            await task.Completion.Task;
        }

        public async Task<T> ExecuteAsync<T>(Func<T> action)
        {
            if (Thread.CurrentThread == _thread)
                return action();

            var task = new QueueTask()
            {
                Action = () => action()!,
                Completion = new TaskCompletionSource<object?>()
            };

            _queue.Enqueue(task);

            var result = await task.Completion.Task;

            return (T)result!;
        }


        public void ProcessQueue()
        {
            if (_isProcessingQueue)
                return;

            if (_thread != Thread.CurrentThread)
                throw new InvalidOperationException("ProcessQueue outside the dispatcher thread");

            _isProcessingQueue = true;

            try
            {
                while (_queue.TryDequeue(out var task))
                {
                    try
                    {
                        var result = task.Action!();

                        task.Completion!.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        task.Completion!.SetException(ex);
                    }
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        public Thread Thread => _thread;
    }
}
