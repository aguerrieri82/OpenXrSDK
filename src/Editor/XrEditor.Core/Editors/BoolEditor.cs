using UI.Binding;

namespace XrEditor
{
    public class BoolEditor : BaseEditor<bool, bool>
    {
        public BoolEditor()
        {
        }

        public BoolEditor(IProperty<bool> binding)
        {
            Binding = binding;
        }

    }
}
