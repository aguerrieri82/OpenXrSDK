
using XrEngine;
using XrEngine.Animation;

namespace XrEditor.Nodes
{
    public class AnimationNode : BaseNode<IAnimation>, IItemView
    {
        public AnimationNode(IAnimation value) 
            : base(value)
        {
        }

        public string DisplayName => _value.Name ?? _value.GetType().Name;

        public IconView? Icon => null;
    }
}
