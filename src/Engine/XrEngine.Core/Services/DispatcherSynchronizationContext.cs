

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
           _dispatcher.Post(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            _dispatcher.ExecuteAsync(() => d(state)).GetAwaiter().GetResult();
        }
    }
}
