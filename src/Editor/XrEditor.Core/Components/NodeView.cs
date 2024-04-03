using System.Collections.ObjectModel;
using XrEditor.Services;

namespace XrEditor
{
    public class NodeView : BaseView
    {
        protected ObservableCollection<object> _children = [];
        protected bool _isExpanded;
        protected readonly OutlinePanel _host;
        protected readonly INode _node;
        protected readonly NodeView? _parent;
        protected bool _isLoaded;
        protected bool _isSelected;
        protected readonly SelectionManager _selection;

        internal NodeView(OutlinePanel host, NodeView? parent, INode node)
        {
            _host = host;   
            _node = node;
            _parent = parent;
            _selection = Context.Require<SelectionManager>();

            if (!node.IsLeaf)
                _children.Add("Loading...");

            if (_selection.IsSelected(node))
                _isSelected = true;
        }

        public void Refresh()
        {

        }

        protected Task LoadChildrenAsync()
        {
            _children.Clear();

            if (!_node.IsLeaf)
            {
                foreach (var item in _node.Children)
                    _children.Add(_host.CreateNodeView(item, this)!);    
            }

            _isLoaded = true;

            return Task.CompletedTask;  
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;
                _isExpanded = value;
                if (_isExpanded && !_isLoaded && !_node.IsLeaf)
                    _ = LoadChildrenAsync();
                OnPropertyChanged(nameof(IsExpanded));  
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;

                bool curSelected = _selection.IsSelected(_node);

                if (!_isSelected && curSelected)
                    _selection.Items.Remove(_node);

                else if (_isSelected && !curSelected)
                    _selection.Items.Add(_node);

                if (_isSelected)
                    _host?._selectedNodes.Add(this);
                else
                    _host?._selectedNodes.Remove(this);

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public IconView? Icon
        {
            get
            {
                if (_node is IItemView itemView)
                    return itemView.Icon;

                return null;
            }
        }

        public string DisplayName
        {
            get
            {
                if (_node is IItemView itemView)
                    return itemView.DisplayName;

                return _node.Value?.ToString() ?? "";
            }
        }

        public NodeView? Parent => _parent;

        public INode Node => _node;

        public ObservableCollection<object> Children => _children;
    }

}
