using System.Reflection;

namespace XrEditor.Services
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
        readonly Dictionary<string, Func<IPanel>> _factories = [];  

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


        public T? Panel<T>() where T : IPanel
        {
            return _panels.OfType<T>().FirstOrDefault();
        }

        public IPanel? Panel(string name) 
        {
            var result = _panels.FirstOrDefault(a => a.PanelId == name);
            if (result == null && _factories.TryGetValue(name, out var factory))
                result = factory();
            return result;
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

        public void Register(Func<IPanel> factory, string panelId)
        {
            _factories[panelId] = factory;  
        }

        public void Register<T>() where T: IPanel, new()
        {
            var attr = typeof(T).GetCustomAttribute<PanelAttribute>();
            if (attr == null)
                throw new NotSupportedException();

            Register(() => new T(), attr.PanelId);  
        }
    }
}
