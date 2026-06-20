using System.Windows;

namespace XrEditor
{
    public class MainDispatcher : IMainDispatcher
    {

        public async Task ExecuteAsync(Action action)
        {
            if (Application.Current == null)
                return;

            if (Thread == Thread.CurrentThread)
                action();
            else
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(action);
                }
                catch (TaskCanceledException)
                {

                }
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<T> action)
        {
            T result = default!;

            await ExecuteAsync(() =>
            {
                result = action();
                return Task.CompletedTask;
            });

            return result;
        }


        public void Execute(Action action, bool force)
        {

            if (!force && Thread == Thread.CurrentThread)
                action();
            else
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
                catch (TaskCanceledException)
                {

                }
            }
        }

        public void Post(Action action)
        {
            Execute(action, false);
        }

        public Thread Thread => Application.Current.Dispatcher.Thread;
    }
}
