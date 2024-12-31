using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class LightNode<T> : Object3DNode<T> where T : Light
    {
        public LightNode(T value) : base(value)
        {

        }

        public override bool IsLeaf => true;

        public override IconView? Icon => new()
        {
            Color = "#FBC02D",
            Name = "icon_lightbulb"
        };

    }
}
