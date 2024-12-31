
namespace XrEditor.Nodes
{
    public class ValueNode : BaseNode<object>, IEditorProperties, IItemView
    {
        public ValueNode(object value) : base(value)
        {

        }

        public void EditorProperties(IList<PropertyView> curProps)
        {
            PropertyView.CreateProperties(_value, _value.GetType(), curProps);
        }

        public string DisplayName => (_value as IItemView)?.DisplayName ?? "";

        public IconView? Icon => (_value as IItemView)?.Icon;

        public bool AutoGenerate { get; set; }

        public override bool IsLeaf => true;


    }
}
