using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Object3DNode<T> : EngineObjectNode<T> where T: Object3D
    {
        NodeDictionary _components = [];


        public Object3DNode(T value)
            : base(value)
        {
            _components.Add(new Transform3DNode(value.Transform));
        }

        public override string DisplayName => _value.Name ?? _value.GetType().Name;

        protected INode GetNode(object value)
        {
            if (!_components.TryGetValue(value, out var node))
            {
                node = Context.Require<NodeFactory>().CreateNode(value);
                _components.Add(node);
            }
            return node;
        }

        public override IEnumerable<INode> Components
        {
            get
            {
                yield return _components[Value.Transform];  
                
                foreach (var component in _value.Components<IComponent>())
                    yield return GetNode(component);
            }
        }
    }
}
