using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class OutlinePanel : BasePanel
    {
        static protected Dictionary<INode, ListTreeNodeView> _listNodeMap = [];

        protected NodeView? _root;
        protected SceneView? _sceneView;
        private ListTreeView _treeView;
        readonly NodeManager _nodeFactory;
        readonly SelectionManager _selection;

        public OutlinePanel()
        {
            Instance = this;
            _nodeFactory = Context.Require<NodeManager>();
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _treeView = new ListTreeView();     
        }

        private void OnSelectionChanged(ListTreeNodeView obj)
        {
            var node = ((NodeView)obj.Header!).Node;

            bool curSelected = _selection.IsSelected(node);

            if (!obj.IsSelected && curSelected)
                _selection.Items.Remove(node);

            else if (obj.IsSelected && !curSelected)
                _selection.Items.Add(node);
        }

        private void OnSelectionChanged(IReadOnlyCollection<INode> newSelection)
        {
            foreach (var curSel in _treeView.SelectedItems.ToArray())
            {
                var node = ((NodeView)curSel.Header!).Node;
                if (!newSelection.Contains(node))
                    curSel.IsSelected = false;
            }

            foreach (var item in newSelection)
            {
                if (_listNodeMap.TryGetValue(item, out var listNode))
                    listNode.IsSelected = true;
            }
        }

        protected internal ListTreeNodeView? CreateNode(object? value, ListTreeNodeView? parent)
        {
            if (value == null)
                return null;

            var node = _nodeFactory.CreateNode(value);

            if (!_listNodeMap.TryGetValue(node, out var listNode))
            {
                listNode =  new ListTreeNodeView(_treeView, parent);
                listNode.SelectionChanged += OnSelectionChanged;
                listNode.Header = new NodeView(listNode, this, node);
                listNode.IsSelected = _selection.IsSelected(node);

                _listNodeMap[node] = listNode;
            }

            return listNode;
        }


        protected override async Task LoadAsync()
        {
            _sceneView = await _panelManager.PanelAsync<SceneView>();
            _sceneView.SceneChanged += OnSceneChanged;

            LoadScene();

            await base.LoadAsync();
        }

        private void OnSceneChanged(Scene3D? obj)
        {
            LoadScene();
        }

        protected void LoadScene()
        {
            var root = CreateNode(_sceneView?.Scene, null);
            _treeView.Items.Clear();
            if (root != null)
                _treeView.Items.Add(root!);
        }

        public static OutlinePanel? Instance { get; internal set; }

        public ListTreeView TreeView => _treeView;
    }
}
