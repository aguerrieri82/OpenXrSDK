using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xr.Editor.Nodes;

namespace Xr.Editor
{
    public class NodeFactory
    {
        List<INodeHandler> _handlers = [];

        public NodeFactory() 
        {
            RegisterHandler(new EngineObjectNodeHandler());
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
