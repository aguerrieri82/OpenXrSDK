using XrEngine;

namespace XrEditor.Nodes
{
    public class EngineObjectNode<T> : BaseNode<T>, IItemView where T : EngineObject
    {
        public EngineObjectNode(T value)
            : base(value)
        {

        }

        public string DisplayName
        {
            get 
            {
                if (_value is Object3D obj && obj.Name != null)
                    return obj.Name;
                return _value.GetType().Name;
            }
        }

        public virtual IconView? Icon => null;
    }
}
