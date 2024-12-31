using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class PanelView : BaseView
    {
        private Guid? _panelId;
        private IPanel? _panel;

        public IPanel? Panel
        {
            get
            {
                if (_panel == null && _panelId != null)
                    _panel = Context.Require<PanelManager>().Panel(_panelId.Value);
                return _panel;
            }
        }

        public Guid? PanelId
        {
            get => _panelId;
            set
            {
                if (_panelId == value)
                    return;
                _panelId = value;
                _panel = null;
                OnPropertyChanged(nameof(PanelId));
                OnPropertyChanged(nameof(Panel));
            }
        }
    }
}
