using System.ComponentModel;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{

    [Panel("6cc81a05-0e2d-4782-b9c3-0d8ab46550ca")]
    [DisplayName("Outline")]
    public class OutlinePanel : BasePanel
    {
        // One INode can be shown in many positions (e.g. a material shared by two meshes):
        // one view per position, all the views of a node are registered here.
        protected readonly Dictionary<INode, List<ListTreeNodeView>> _listNodeMap = [];

        protected SceneView? _sceneView;
        protected readonly ListTreeView _treeView;
        protected readonly NodeManager _nodeFactory;
        protected readonly SelectionManager _selection;

        public OutlinePanel()
        {
            Instance = this;
            _nodeFactory = Context.Require<NodeManager>();
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnManagerSelectionChanged;
            _treeView = new ListTreeView();
        }

        public IReadOnlyList<ListTreeNodeView> FindViews(INode node)
        {
            return _listNodeMap.TryGetValue(node, out var views) ? views : [];
        }

        /// <summary>
        /// Expands the ancestors of target (following INode.Parent) and scrolls to its view.
        /// Cost is proportional to the depth, the tree is not scanned: at each level the view
        /// whose parent is the view chosen at the previous level is taken from the map.
        /// Expanding a level creates (and registers) the views of the next level.
        /// </summary>
        public ListTreeNodeView? ExpandNode(INode target)
        {
            var path = new List<INode>();
            for (var curNode = target; curNode != null; curNode = curNode.Parent)
                path.Insert(0, curNode);

            ListTreeNodeView? curView = null;
            ListTreeNodeView? targetView = null;

            foreach (var node in path)
            {
                if (!_listNodeMap.TryGetValue(node, out var views) || views.Count == 0)
                    continue;

                var parentView = curView;

                curView = views.FirstOrDefault(a => a.Parent == parentView) ??
                          views.FirstOrDefault(a => a.IsVisible) ??
                          views[0];

                if (node == target)
                    targetView = curView;
                else
                    curView.IsExpanded = true;
            }

            if (targetView != null)
                _treeView.ScrollIntoView(targetView);

            return targetView;
        }

        private async void OnNodeSelectionChanged(ListTreeNodeView obj)
        {
            await UiThread;

            var node = ((NodeView)obj.Header!).Node;

            var curSelected = _selection.IsSelected(node);

            //Log.Debug(this, "[Sel-Node] OnSelectionChanged: {0} ({1})", obj.IsSelected, obj.Header);

            if (!obj.IsSelected && curSelected)
                _selection.Items.Remove(node);

            else if (obj.IsSelected && !curSelected)
                _selection.Items.Add(node);
        }

        private async void OnManagerSelectionChanged(IReadOnlyCollection<INode> newSelection)
        {
            await UiThread;

            //Log.Debug(this, "[Sel-Man] OnSelectionChanged: {0}", newSelection.Count);

            foreach (var curSel in _treeView.SelectedItems.ToArray())
            {
                var node = ((NodeView)curSel.Header!).Node;
                if (!newSelection.Contains(node))
                    curSel.SetSelectedCore(false, false);
            }

            // all the positions of a selected node are selected
            foreach (var item in newSelection)
            {
                if (_listNodeMap.TryGetValue(item, out var views))
                {
                    foreach (var view in views.ToArray())
                        view.SetSelectedCore(true, false);
                }
            }

            _mainDispatcher.Execute(() =>
            {
                if (newSelection.Count == 1)
                    ExpandNode(newSelection.First());
            }, true);

        }

        protected internal ListTreeNodeView? CreateNode(object? value, ListTreeNodeView? parent)
        {
            if (value == null)
                return null;

            var node = _nodeFactory.CreateNode(value);

            // Always a new view: a view is a position in the tree, never reused
            var listNode = new ListTreeNodeView(_treeView, parent);
            listNode.Header = new NodeView(listNode, this, node);
            listNode.SetSelectedCore(_selection.IsSelected(node), false);
            listNode.SelectionChanged += OnNodeSelectionChanged;
            listNode.Removed += OnNodeRemoved;

            if (!_listNodeMap.TryGetValue(node, out var views))
                _listNodeMap[node] = views = [];

            views.Add(listNode);

            return listNode;
        }

        private void OnNodeRemoved(ListTreeNodeView listNode)
        {
            listNode.SelectionChanged -= OnNodeSelectionChanged;
            listNode.Removed -= OnNodeRemoved;

            var nodeView = (NodeView)listNode.Header!;

            if (_listNodeMap.TryGetValue(nodeView.Node, out var views))
            {
                views.Remove(listNode);
                if (views.Count == 0)
                    _listNodeMap.Remove(nodeView.Node);
            }

            nodeView.Dispose(); // unsubscribe from the INode events
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
            _treeView.SetRoot(root);
        }

        public static OutlinePanel? Instance { get; internal set; }

        public ListTreeView TreeView => _treeView;

        public override string? Title => "Outline";
    }
}
