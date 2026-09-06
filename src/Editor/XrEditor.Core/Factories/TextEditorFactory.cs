namespace XrEditor
{
    public readonly struct TextEditorFactory<T> : IPropertyEditorFactory
    {
        readonly Func<string, T> _parser;
        readonly Func<T, string> _formatter;

        public TextEditorFactory(Func<string, T> parser, Func<T, string>? formatter = null)
        {
            _parser = parser;
            _formatter = formatter ?? new Func<T, string>(a => a?.ToString() ?? "");
        }

        public readonly bool CanHandle(Type type)
        {
            return type == typeof(T);
        }

        public readonly IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            return (IPropertyEditor)Activator.CreateInstance(typeof(TextEditor<>).MakeGenericType(type), _parser, _formatter)!;
        }
    }
}
