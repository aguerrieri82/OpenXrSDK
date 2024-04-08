using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class OutlinePanel : BasePanel
    {
        static protected Dictionary<INode, NodeView> _nodeMap = [];

        protected NodeView? _root;
        protected SceneView? _sceneView;
        protected internal HashSet<NodeView> _selectedNodes = [];

        readonly NodeManager _nodeFactory;
        readonly SelectionManager _selection;

        public OutlinePanel()
        {
            Instance = this;
            _nodeFactory = Context.Require<NodeManager>();
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
        }

        private void OnSelectionChanged(IReadOnlyCollection<INode> items)
        {
            foreach (var curSel in _selectedNodes.ToArray())
                curSel.IsSelected = false;

            foreach (var item in items)
            {
                if (_nodeMap.TryGetValue(item, out var nodeView))
                    nodeView.IsSelected = true;
            }
        }

        protected internal NodeView? CreateNodeView(object? value, NodeView? parent)
        {
            if (value == null)
                return null;

            var node = _nodeFactory.CreateNode(value);

            if (!_nodeMap.TryGetValue(node, out var nodeView))
            {
                nodeView = new NodeView(this, parent, node);
                _nodeMap[node] = nodeView;
            }

            return nodeView;
        }

        public NodeView? Root
        {
            get => _root;
            set
            {
                if (_root == value)
                    return;
                _root = value;
                OnPropertyChanged(nameof(Root));
                OnPropertyChanged(nameof(RootChildren));
            }
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
            Root = CreateNodeView(_sceneView?.Scene, null);
        }

        public static OutlinePanel? Instance { get; internal set; }

        public NodeView[] RootChildren => _root != null ? [_root] : [];
    }
}
