using System.Collections.Concurrent;
using System.Diagnostics;

namespace OpenXr.Framework
{
    public class DispatcherAction
    {
        public Func<object?>? Action { get; set; }

        public TaskCompletionSource<object?>? CompletionSource { get; set; }
    }

    public class XrDispatcherThread : IXrThread
    {
        readonly ConcurrentQueue<DispatcherAction> _actions = new();

        public void ProcessQueue()
        {
            while (_actions.TryDequeue(out DispatcherAction? item))
            {
                Debug.Assert(item.CompletionSource != null);
                Debug.Assert(item.Action != null);

                try
                {
                    object? result = item.Action();
                    item.CompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    item.CompletionSource.SetException(ex);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<T> action)
        {
            DispatcherAction item = new DispatcherAction
            {
                Action = () => action(),
                CompletionSource = new TaskCompletionSource<object?>()
            };

            _actions.Enqueue(item);

            await item.CompletionSource.Task;

            return (T)item.CompletionSource.Task.Result!;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            Task<T> task = await ExecuteAsync<Task<T>>(action);
            return await task;
        }
    }
}
