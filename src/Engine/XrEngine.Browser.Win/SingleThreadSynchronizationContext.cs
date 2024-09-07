using System.Collections.Concurrent;

namespace XrEngine.Browser.Win
{
    public sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object?>> queue = [];

        public override void Post(SendOrPostCallback d, object? state)
        {
            queue.Add(new KeyValuePair<SendOrPostCallback, object?>(d, state));
        }

        public void RunOnCurrentThread()
        {
            while (queue.TryTake(out var workItem, Timeout.Infinite))
            {
                workItem.Key(workItem.Value);
            }
        }

        public void Complete()
        {
            queue.CompleteAdding();
        }
    }
}
