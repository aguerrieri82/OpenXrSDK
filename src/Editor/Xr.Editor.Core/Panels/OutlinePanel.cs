using System.Numerics;
using Xr.Engine;

namespace Xr.Editor
{
    public class OutlinePanel : BasePanel
    {
        private NodeView? _root;

        readonly NodeFactory _nodeFactory;

        public OutlinePanel()
        {
            Instance = this;
            _nodeFactory = Context.Require<NodeFactory>();
            _root = CreateNodeView(EngineApp.Current?.ActiveScene);
        }

        protected NodeView? CreateNodeView(object? value)
        {
            if (value == null)
                return null;
            var node = _nodeFactory.CreateNode(value);
            return new NodeView(this, node);
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
            }
        }

        public static OutlinePanel? Instance { get; internal set; }
    }
}
