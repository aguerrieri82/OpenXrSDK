using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var factory = Context.Require<NodeFactory>();
                return _value.Materials.Select(a => factory.CreateNode(a));
            }
        }

        public override IconView? Icon => new()
        {
            Color = "#388E3C",
            Name = "icon_view_in_ar"
        };
    }
}
