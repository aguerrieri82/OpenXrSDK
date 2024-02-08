using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class DispatcherAction
    {
        public Func<object?>? Action { get; set; }

        public TaskCompletionSource<object?>? CompletionSource { get; set;}
    }

    public class DispatcherXrThread : IXrThread
    {
        ConcurrentQueue<DispatcherAction> _actions = new();

        public void ProcessQueue()
        {
            while (_actions.TryDequeue(out var item))
            {
                try
                {
                    var result = item.Action!();
                    item.CompletionSource!.SetResult(result);
                }
                catch (Exception ex)
                {
                    item.CompletionSource!.SetException(ex);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<T> action)
        {
            var item = new DispatcherAction
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
            var task = await ExecuteAsync<Task<T>>(action);
            return await task;
        }
    }
}
