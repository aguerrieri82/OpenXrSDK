namespace XrEditor.Nodes
{
    public abstract class BaseNode<T> : INode where T : notnull
    {
        protected INode? _parent;
        protected T _value;
        protected string[]? _types;

        public BaseNode(T value)
        {
            _value = value;
        }

        protected virtual string[] ComputeType(object value)
        {
            var result = new List<string>();

            var curType = value.GetType()!;

            while (curType != typeof(object))
            {
                result.Add(curType!.Name);
                curType = curType.BaseType;
            }

            return [.. result];
        }

        public virtual bool IsLeaf => false;

        public virtual IEnumerable<INode> Children => [];

        public virtual IEnumerable<INode> Components => [];

        public ICollection<string> Types
        {
            get
            {
                _types ??= ComputeType(_value);
                return _types;
            }
        }

        public T Value => _value;

        object INode.Value => _value;

        public INode? Parent => _parent;
    }
}
