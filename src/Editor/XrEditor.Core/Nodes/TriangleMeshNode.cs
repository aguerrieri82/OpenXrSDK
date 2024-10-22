using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class TriangleMeshNode : Object3DNode<TriangleMesh>
    {
        public TriangleMeshNode(TriangleMesh value) : base(value)
        {
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();

                foreach (var material in _value.Materials)
                    yield return factory.CreateNode(material);

                if (_value.Geometry != null)
                    yield return factory.CreateNode(_value.Geometry);
            }
        }

        public override IconView? Icon => new()
        {
            Color = "#388E3C",
            Name = "icon_view_in_ar"
        };
    }
}
