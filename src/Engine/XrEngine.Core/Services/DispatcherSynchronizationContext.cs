

namespace XrEngine.Services
{
    public sealed class DispatcherSynchronizationContext : SynchronizationContext
    {
        readonly IDispatcher _dispatcher;

        public DispatcherSynchronizationContext(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _ = _dispatcher.ExecuteAsync(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _dispatcher.Thread)
            {
                d(state);
                return;
            }

            _dispatcher.ExecuteAsync(() => d(state)).GetAwaiter().GetResult();
        }
    }
}
