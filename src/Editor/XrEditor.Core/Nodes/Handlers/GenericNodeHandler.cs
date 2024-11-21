using XrEngine;
using XrEngine.Physics;

namespace XrEditor.Nodes
{
    public class GenericNodeHandler : INodeHandler
    {
        public bool CanHandle(object value)
        {
            return true;
        }

        public INode CreateNode(object value)
        {
            return new ValueNode(value);
        }
    }
}
