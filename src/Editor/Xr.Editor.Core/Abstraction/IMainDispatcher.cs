using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Editor
{
    public interface IMainDispatcher
    {
        Task ExecuteAsync(Action action);

    }
}
