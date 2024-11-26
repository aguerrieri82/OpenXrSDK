
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
        protected Guid _panelId;
        protected IPanelContainer? _container;
        protected bool _isActive;

        public BasePanel()
        {
            _ui = Context.Require<IUserInteraction>();
            _mainDispatcher = Context.Require<IMainDispatcher>();
            _panelManager = Context.Require<PanelManager>();

            var panelAttr = GetType().GetCustomAttribute<PanelAttribute>();

            if (panelAttr != null)
                _panelId = panelAttr.PanelId;

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

        public virtual void OnActivate()
        {

        }

        public void Attach(IPanelContainer container)
        {
            _container = container;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;

                _isActive = value;

                if (_isActive)
                {
                    OnActivate();
                    if (_container != null)
                        _container.ActivePanel = this;
                }
            }
        }

        public Guid PanelId => _panelId;

        public abstract string? Title { get; }

        public ToolbarView? ToolBar { get; set; }
     
        public IPanelContainer? Container => _container;
    }
}
