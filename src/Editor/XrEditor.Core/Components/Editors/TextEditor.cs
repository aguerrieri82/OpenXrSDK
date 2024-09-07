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

    public struct TextEditorFactory<T> : IPropertyEditorFactory
    {
        Func<string, T> _parser;
        Func<T, string> _formatter;

        public TextEditorFactory(Func<string, T> parser, Func<T, string>? formatter = null)
        {
            _parser = parser;
            _formatter = formatter ?? new Func<T, string>(a => a?.ToString() ?? "");
        }

        public bool CanHandle(Type type)
        {
            return type == typeof(T);
        }

        public IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes)
        {
            return (IPropertyEditor)Activator.CreateInstance(typeof(TextEditor<>).MakeGenericType(type), _parser, _formatter)!;
        }
    }
}
