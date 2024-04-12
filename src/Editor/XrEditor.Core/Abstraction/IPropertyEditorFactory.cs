namespace XrEditor
{
    public interface IPropertyEditorFactory
    {
        bool CanHandle(Type type);

        IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes);
    }
}
