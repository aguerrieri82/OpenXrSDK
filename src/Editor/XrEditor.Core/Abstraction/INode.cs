namespace XrEditor
{
    public interface INode
    {
        IEnumerable<INode> Children { get; }

        IEnumerable<INode> Components { get; }

        public ICollection<string> Types { get; }

        public object Value { get; }

        public INode? Parent { get; }

        public bool IsLeaf { get; }
    }

    public interface IEditableNode : INode
    {
        void SetParent(INode? parent);  
    }
}
