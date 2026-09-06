using XrEngine;

namespace XrEditor
{
    public readonly struct EngineObjectEditorFactory : IPropertyEditorFactory
    {
        public readonly bool CanHandle(Type type)
        {
            return typeof(EngineObject).IsAssignableFrom(type);
        }

        public readonly IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            return new ElementPicker();
        }
    }
}
