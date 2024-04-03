using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public class QueueDispatcher : IDispatcher
    {
        public class QueueTask
        {
            public Func<object?>? Action;

            public TaskCompletionSource<object?>? Completion;
        }

        readonly ConcurrentQueue<QueueTask> _queue = [];
        protected bool _isProcessingQueue;

        public async Task ExecuteAsync(Action action)
        {
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
    }
}
