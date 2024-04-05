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
