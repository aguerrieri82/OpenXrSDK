using UI.Binding;

namespace XrEditor
{
    public interface ITextEditor : IPropertyEditor
    {

    }

    public class TextEditor<TValue> : BaseEditor<TValue, string>, ITextEditor
    {

        public TextEditor()
        {
        }

        public TextEditor(Func<string, TValue> parser, Func<TValue, string> formatter)
        {
            Parser = parser;
            Formatter = formatter;
        }

        public TextEditor(IProperty<TValue> binding)
        {
            Binding = binding;
        }

        protected override string BindToEditValue(TValue value)
        {
            if (Formatter != null)
                return Formatter(value);
            return base.BindToEditValue(value);
        }

        protected override TValue EditValueToBind(string value)
        {
            if (Parser != null)
                return Parser(value);
            return base.EditValueToBind(value);
        }

        public Func<string, TValue>? Parser { get; set; }

        public Func<TValue, string>? Formatter { get; set; }
    }
}
