using System.Windows;

namespace Xr.Editor
{
    public class MainDispatcher : IMainDispatcher
    {
        public async Task ExecuteAsync(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }
    }
}
