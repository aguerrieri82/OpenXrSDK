using System.ComponentModel;
using System.Reflection;

namespace XrEditor.Services
{

    public class PanelInfo
    {
        public PanelInfo(Func<IPanel> factory, Guid panelId)
        {
            PanelId = panelId;
            Factory = factory;
        }


        public Guid PanelId { get; }

        public Func<IPanel> Factory { get; }

        public string? Title { get; set; }

        public string? Icon { get; set; }

        public IPanel? Instance { get; set; }
    }

    public class PanelManager
    {
        class PanelLoadListener
        {
            public TaskCompletionSource<IPanel>? TaskSource;

            public Type? PanelType;
        }

        readonly List<IPanel> _panels = [];
        readonly List<PanelLoadListener> _loadListeners = [];
        readonly Dictionary<Guid, PanelInfo> _infos = [];

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

        public IPanel? Panel(Guid panelId)
        {
            var result = _panels.FirstOrDefault(a => a.PanelId == panelId);
            if (result == null && _infos.TryGetValue(panelId, out var info))
            {
                info.Instance ??= info.Factory();
                return info.Instance;
            }
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


        public void Register(PanelInfo info)
        {
            _infos[info.PanelId] = info;
        }


        public void Register(Func<IPanel> factory, Guid panelId, string? title = null)
        {
            Register(new PanelInfo(factory, panelId)
            {
                Title = title,
            });
        }

        public void Register<T>() where T : IPanel, new()
        {
            var panelAttr = typeof(T).GetCustomAttribute<PanelAttribute>();
            var displayAttr = typeof(T).GetCustomAttribute<DisplayNameAttribute>();
            if (panelAttr == null)
                throw new NotSupportedException($"Panel attribute not declared in type {typeof(T).FullName}");

            Register(() => new T(), panelAttr.PanelId, displayAttr?.DisplayName);
        }


        public IReadOnlyList<IPanel> Panels => _panels;

        public IReadOnlyCollection<PanelInfo> PanelsInfo => _infos.Values;


    }
}
