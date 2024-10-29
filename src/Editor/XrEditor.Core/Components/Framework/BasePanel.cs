
using System.Reflection;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public abstract class BasePanel : BaseView, IPanel, IStateManager
    {
        protected readonly PanelManager _panelManager;
        protected readonly IUserInteraction _ui;
        protected readonly IMainDispatcher _main;
        protected string _panelId;

        public BasePanel()
        {
            _ui = Context.Require<IUserInteraction>();
            _main = Context.Require<IMainDispatcher>();
            _panelManager = Context.Require<PanelManager>();

            var panelAttr = GetType().GetCustomAttribute<PanelAttribute>(); 
            _panelId = panelAttr?.PanelId ?? GetType().Name;

            _ = LoadAsync();
        }

        protected virtual Task LoadAsync()
        {
            _panelManager.NotifyLoaded(this);

            return Task.CompletedTask;
        }

        public virtual Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void GetState(IStateContainer container)
        {
            throw new NotImplementedException();
        }

        public virtual void SetState(IStateContainer container)
        {
            throw new NotImplementedException();
        }

        public string PanelId => _panelId;      
    }
}
