using UI.Binding;

namespace XrEditor
{
    public class TextEditor : BaseEditor<string, string>
    {
        public TextEditor()
        {
        }

        public TextEditor(IProperty<string> binding)
        {
            Binding = binding;
        }

    }
}
