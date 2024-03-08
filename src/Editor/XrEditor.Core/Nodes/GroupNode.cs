using XrEngine;

namespace XrEditor.Nodes
{
    internal class GroupNode : BaseNode<Group3D>
    {
        public GroupNode(Group3D value)
            : base(value)
        {

        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeFactory>();
                return _value.Children.Select(a => factory.CreateNode(a));
            }
        }
    }
}
