using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class TriangleMeshNode : Object3DNode<TriangleMesh>
    {
        GroupNode? _joints;

        public TriangleMeshNode(TriangleMesh value) : base(value)
        {
        }

        public override IEnumerable<INode> Children
        {
            get
            {
                var factory = Context.Require<NodeManager>();

                foreach (var material in _value.Materials)
                    yield return factory.CreateNode(material, this);

                if (_value.Geometry != null)
                    yield return factory.CreateNode(_value.Geometry);

                var skin = _value.Feature<ISkinnedMesh>();

                if (skin?.Joints != null)
                {
                    _joints ??= new GroupNode(skin.Joints.Select(a => factory.CreateNode(a)), "Joints");
                    _joints.SetParent(this);
                    yield return _joints;
                }

            }
        }

        public override IconView? Icon => new()
        {
            Color = "#388E3C",
            Name = "icon_view_in_ar"
        };
    }
}
