using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Group3DNode<T> : Object3DNode<T> where T : Group3D
    {
        public Group3DNode(T value)
            : base(value)
        {

        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();
                return _value.Children.Select(a => factory.CreateNode(a));
            }
        }


        public override IconView? Icon => new()
        {
            Color = "#1565C0",
            Name = "icon_category"
        };
    }
}
