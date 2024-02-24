using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
