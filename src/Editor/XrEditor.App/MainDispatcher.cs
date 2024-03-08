using System.Windows;

namespace XrEditor
{
    public class MainDispatcher : IMainDispatcher
    {
        public async Task ExecuteAsync(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }
    }
}
