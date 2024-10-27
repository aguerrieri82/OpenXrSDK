using System.Windows;

namespace XrEditor
{
    public class MainDispatcher : IMainDispatcher
    {
        public async Task ExecuteAsync(Action action)
        {

            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
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
    }
}
