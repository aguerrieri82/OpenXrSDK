using System.Collections.ObjectModel;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class PanelContainer : BaseView, IStateManager, IPanelContainer
    {
        private IPanel? _activePanel;
        private bool _isActive;

        public PanelContainer()
            : this([])
        {
        }

        public PanelContainer(params IPanel[] panels)
        {
            Panels = [];
            Panels.CollectionChanged += OnPanelsChanged;
            foreach (var panel in panels)
                Panels.Add(panel);
            ActivePanel = panels.FirstOrDefault();
            Menu = new MenuView();
            FillMenu();
            CloseCommand = new Command(CloseAsync);
        }

        private void FillMenu()
        {
            var addGrp = Menu.AddGroup("Add");
            var pm = Context.Require<PanelManager>();
            foreach (var info in pm.PanelsInfo)
            {
                addGrp.AddButton("", () =>
                {
                    var instance = pm.Panel(info.PanelId);
                    if (instance == null)
                        return;

                    if (!Panels.Contains(instance))
                        Panels.Add(instance);

                    Context.Require<IMainDispatcher>().Execute(() =>
                    {
                        ActivePanel = instance;
                    });
     

                }, info.Title);
            }
        }

        private void OnPanelsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<IPanel>())
                    item.Attach(this);

            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<IPanel>())
                {
                    if (_activePanel == item)
                        ActivePanel = Panels.FirstOrDefault();
                }
            }
        }

        public void GetState(IStateContainer container)
        {
            var panels = container.Enter("Panels");
            int i = 0;
            foreach (var panel in Panels)
            {
                var panelState = panels.Enter(i.ToString());
                panelState.Write("PanelId", panel.PanelId);
                if (panel is IStateManager state)
                    state.GetState(panelState);
                i++;
            }
            container.Write("ActivePanel", ActivePanel == null ? -1 : Panels.IndexOf(ActivePanel));
        }

        public Task CloseAsync()
        {
            if (_activePanel == null)
                return Task.CompletedTask;
            
            Panels.Remove(_activePanel);
            return Task.CompletedTask;

            //return _activePanel.CloseAsync();
        }

        public void SetState(IStateContainer container)
        {
            Panels.Clear();

            var manager = Context.Require<PanelManager>();
            var panels = container.Enter("Panels");

            foreach (var key in panels.Keys)
            {
                var panelState = panels.Enter(key);
                var panelId = panelState.Read<Guid>("PanelId");
                var panel = manager.Panel(panelId);
                if (panel == null)
                    throw new Exception("");
                if (panel is IStateManager state)
                    state.SetState(panelState);
                Panels.Add(panel);
            }

            var activePanel = container.Read<int>("ActivePanel");

            if (activePanel == -1 && Panels.Count > 0)
                activePanel = 0;

            if (activePanel != -1)
                ActivePanel = Panels[activePanel];
        }

        public void Remove(IPanel panel)
        {
            Panels.Remove(panel);
        }

        public IPanel? ActivePanel
        {
            get => _activePanel;
            set
            {
                if (_activePanel == value)
                    return;
                if (_activePanel != null)
                    _activePanel.IsActive = false;
                _activePanel = value;
                if (_activePanel != null)
                    _activePanel.IsActive = true;
                OnPropertyChanged(nameof(ActivePanel));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }


        public Command CloseCommand { get; }

        public MenuView Menu { get; }

        public ObservableCollection<IPanel> Panels { get; }

    }
}
