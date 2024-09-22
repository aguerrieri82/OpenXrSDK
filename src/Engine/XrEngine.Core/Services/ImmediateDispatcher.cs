using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Services
{
    public class ImmediateDispatcher : IDispatcher
    {
        public Task ExecuteAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            return Task.FromResult(action());
        }
    }
}
