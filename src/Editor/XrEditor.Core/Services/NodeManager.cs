using XrEditor.Nodes;

namespace XrEditor.Services
{
    public class NodeManager
    {
        readonly List<INodeHandler> _handlers = [];

        public NodeManager()
        {
            RegisterHandler(new EngineObjectNodeHandler());
            RegisterHandler(new GenericNodeHandler());
        }

        public INode CreateNode(object value)
        {
            if (value is INode node)
                return node;

            if (value is INodeProvider provider)
                return provider.Node;

            var handler = _handlers.First(a => a.CanHandle(value));
            if (handler != null)
                return handler.CreateNode(value);

            throw new NotSupportedException();
        }

        public void RegisterHandler(INodeHandler handler)
        {
            _handlers.Add(handler);
        }
    }
}
