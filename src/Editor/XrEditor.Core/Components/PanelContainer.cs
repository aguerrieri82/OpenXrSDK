using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class PanelContainer : BaseView, IStateManager
    {
        private IPanel? _activePanel;

        public PanelContainer()
        {
            Panels = [];    
        }

        public PanelContainer(params IPanel[] panels)
        {
            Panels = [..panels];
            ActivePanel = panels[0];
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

        public void SetState(IStateContainer container)
        {
            Panels.Clear();

            var manager = Context.Require<PanelManager>();
            var panels = container.Enter("Panels");

            foreach (var key in panels.Keys)
            {
                var panelState = panels.Enter(key);
                var panelId = panelState.Read<string>("PanelId");
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

        public IPanel? ActivePanel
        {
            get => _activePanel;
            set
            {
                if (_activePanel == value)
                    return;
                _activePanel = value;
                OnPropertyChanged(nameof(ActivePanel));
            }
        }

        public ObservableCollection<IPanel> Panels { get; }

    }
}
