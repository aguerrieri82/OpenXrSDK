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
                await Application.Current.Dispatcher.InvokeAsync(action);
        }
    }
}
