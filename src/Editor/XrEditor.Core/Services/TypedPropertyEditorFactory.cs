namespace XrEditor.Services
{
    public struct TypedPropertyEditorFactory<T, TEditor> : IPropertyEditorFactory where TEditor : BaseEditor<T, T>, new()
    {
        public readonly bool CanHandle(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public readonly IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            var result = new TEditor();
            result.SetAttributes(attributes);
            result.Host = host;
            return result;
        }
    }
}
