namespace XrEditor
{
    public readonly struct EnumEditorFactory : IPropertyEditorFactory
    {
        public readonly bool CanHandle(Type type)
        {
            return type.IsEnum;
        }

        public readonly IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            return (IPropertyEditor)Activator.CreateInstance(typeof(EnumEditor<>).MakeGenericType(type))!;
        }
    }
}
