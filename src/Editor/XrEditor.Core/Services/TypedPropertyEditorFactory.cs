namespace XrEditor.Services
{
    public struct TypedPropertyEditorFactory<T, TEditor> : IPropertyEditorFactory where TEditor : BaseEditor<T, T>, new()
    {
        public readonly bool CanHandle(Type type)
        {
            return typeof(T) == type;
        }

        public readonly IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes)
        {
            return new TEditor();
        }
    }
}
