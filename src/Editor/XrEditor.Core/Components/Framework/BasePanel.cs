
using System.Reflection;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public abstract class BasePanel : BaseView, IPanel, IStateManager
    {
        protected readonly PanelManager _panelManager;
        protected readonly IUserInteraction _ui;
        protected readonly IMainDispatcher _mainDispatcher;
        protected string _panelId;
        protected IPanelContainer? _container;
        protected bool _isActive;

        public BasePanel()
        {
            _ui = Context.Require<IUserInteraction>();
            _mainDispatcher = Context.Require<IMainDispatcher>();
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

        }

        public virtual void SetState(IStateContainer container)
        {
        }

        public virtual void NotifyActivated(IPanelContainer container, bool isActive)
        {
            _container = container;
            _isActive = isActive;
        }

        public string PanelId => _panelId;

        public abstract string? Title { get; }


        public ToolbarView? ToolBar { get; set; }
    }
}
