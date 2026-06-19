using System.Runtime.CompilerServices;

namespace XrEngine
{
    public readonly struct DispatcherSwitch : ICriticalNotifyCompletion
    {
        readonly IDispatcher _dispatcher;

        public DispatcherSwitch(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public DispatcherSwitch GetAwaiter() => this;

        public void GetResult() { }

        public bool IsCompleted => _dispatcher.Thread == Thread.CurrentThread;

        public void OnCompleted(Action continuation)
        {
            _dispatcher.Post(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _dispatcher.Post(continuation);
        }
    }
}
