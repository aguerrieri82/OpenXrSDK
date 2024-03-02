namespace Xr.Editor
{
    public class PanelManager
    {
        class PanelLoadListener
        {
            public TaskCompletionSource<IPanel>? TaskSource;

            public Type? PanelType;
        }

        readonly List<IPanel> _panels = [];
        readonly List<PanelLoadListener> _loadListeners = [];

        public void NotifyLoaded(IPanel panel)
        {
            _panels.Add(panel);

            for (var i = _loadListeners.Count - 1; i >= 0; i--)
            {
                if (_loadListeners[i].PanelType!.IsAssignableFrom(panel.GetType()))
                {
                    _loadListeners[i].TaskSource!.SetResult(panel);
                    _loadListeners.RemoveAt(i);
                }
            }
        }


        public async Task<T> PanelAsync<T>() where T : IPanel
        {
            var panel = _panels.OfType<T>().FirstOrDefault();
            if (panel != null)
                return panel;

            var source = new TaskCompletionSource<IPanel>();

            _loadListeners.Add(new PanelLoadListener
            {
                TaskSource = source,
                PanelType = typeof(T)
            });

            return (T)await source.Task;
        }


        public async Task CloseAllAsync()
        {
            await Task.WhenAll(_panels.Select(a => a.CloseAsync()));
        }
    }
}
