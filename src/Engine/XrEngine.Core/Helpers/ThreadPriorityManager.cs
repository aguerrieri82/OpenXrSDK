using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public readonly struct ThreadPriorityManager : IDisposable
    {
        readonly ThreadPriority _oldPriority;
        readonly Thread _thread;

        ThreadPriorityManager(ThreadPriority newPriority)
        {
            _thread = Thread.CurrentThread;
            _oldPriority = _thread.Priority;
            _thread.Priority = newPriority;
        }

        public void Dispose()
        {
            _thread.Priority = _oldPriority;
        }

        public static ThreadPriorityManager Switch(ThreadPriority priority)
        {
            return new ThreadPriorityManager(priority);
        }

    }
}
