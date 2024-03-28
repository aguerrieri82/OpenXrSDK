using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor.Nodes
{
    public class ValueNode : BaseNode<object>
    {
        public ValueNode(object value) : base(value)
        {
        }

        public override bool IsLeaf => true;

    }
}
