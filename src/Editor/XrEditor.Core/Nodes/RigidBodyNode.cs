using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Physics;

namespace XrEditor.Nodes
{
    public class RigidBodyNode : ComponentNode<RigidBody>
    {
        public RigidBodyNode(RigidBody value) : base(value)
        {
        }
    }
}
