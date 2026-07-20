using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class SplatMeshNode : Object3DNode<SplatMesh>
    {
        public SplatMeshNode(SplatMesh value) : base(value)
        {
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();

                yield return factory.CreateNode(_value.Material);
            }
        }

        public override IconView? Icon => new()
        {
            Color = "#388E3C",
            Name = "icon_view_in_ar"
        };
    }
}
