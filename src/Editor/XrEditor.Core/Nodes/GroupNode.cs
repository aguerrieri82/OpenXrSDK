
namespace XrEditor.Nodes
{
    public class GroupNode : BaseNode<object>, IItemView
    {
        protected IEnumerable<INode> _children;
        protected string _displayName;
        protected IconView _icon;

        public GroupNode(IEnumerable<INode> children, string displayName) : base(new object())
        {
            _children = children;
            _displayName = displayName;
            _icon = new IconView
            {
                Color = "#ffff00",
                Name = "icon_folder"
            };
        }

        public override IEnumerable<INode> Children => _children;

        public string DisplayName => _displayName;

        public IconView? Icon => _icon;

        public override bool IsLeaf => false;

    }
}
