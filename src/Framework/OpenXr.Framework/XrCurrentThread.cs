using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrCurrentThread : IXrThread
    {
        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            return Task.FromResult(action());   
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            return action();
        }
    }
}
