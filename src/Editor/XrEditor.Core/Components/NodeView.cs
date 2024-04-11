using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class NodeView : IDisposable
    {
        static readonly MenuView _menu = new();
        protected readonly INode _node;
        protected OutlinePanel _panel;
        protected ListTreeNodeView _host;

        internal NodeView(ListTreeNodeView host, OutlinePanel panel, INode node)
        {
            _host = host;
            _node = node;
            _panel = panel;


            if (_node is IDynamicNode dynamicNode)
            {
                dynamicNode.ChildAdded += OnChildAdded;
                dynamicNode.ChildRemoved += OnChildRemoved;
            }

            _host.IsLeaf = _node.IsLeaf;
            _host.LoadChildren += OnLoadChildren;
        }

        public void UpdateMenu()
        {
            _menu.Items.Clear();

            if (!_node.IsLeaf)
            {
                _menu.AddButton("icon_refresh", _host.Refresh, "Refresh");
                _menu.AddDivider();
            }

            if (_node is IItemActions itemActions)
            {
                var result = new List<ActionView>();
                itemActions.Actions(result);
                foreach (var item in result)
                    _menu.Items.Add(item);
            }
        }

        protected void OnChildRemoved(INode sender, INode child)
        {
            Context.Require<IMainDispatcher>().ExecuteAsync(() =>
            {
                var childView = _host.Children!.FirstOrDefault(a => ((NodeView)a.Header!).Node == child);
                childView?.Remove();
            });
        }

        protected void OnChildAdded(INode sender, INode child)
        {
            Context.Require<IMainDispatcher>().ExecuteAsync(() =>
            {
                var childView = _panel.CreateNode(child, _host);
                _host.AddChild(childView!);
            });
        }


        protected void OnLoadChildren(ListTreeNodeView nodeView, IList<ListTreeNodeView> result)
        {
            if (_node.IsLeaf)
                return;

            foreach (var item in _node.Children)
                result.Add(_panel.CreateNode(item, _host)!);
        }


        public void Dispose()
        {
            if (_node is IDynamicNode dynamicNode)
            {
                dynamicNode.ChildAdded -= OnChildAdded;
                dynamicNode.ChildRemoved -= OnChildRemoved;
            }
            GC.SuppressFinalize(this);
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

        public MenuView Menu => _menu;

        public INode Node => _node;
    }

}
