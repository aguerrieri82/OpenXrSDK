using UI.Binding;
using XrEngine;

namespace XrEditor.Nodes
{
    public class ShaderNode<T> : EngineObjectNode<T> where T : Shader
    {
        public ShaderNode(T value)
            : base(value)
        {

        }

        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            PropertyView.CreateProperties(_value, typeof(Shader), curProps);
            PropertyView.CreateProperties(_value, typeof(T), curProps);
        }

        public override IconView? Icon => new()
        {

            Color = "#aaaaaa",
            Name = "icon_fluorescent"
        };

    }
}
