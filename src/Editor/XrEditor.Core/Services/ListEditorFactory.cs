using System.Collections;

namespace XrEditor.Services
{
    internal class ListEditorFactory : IPropertyEditorFactory
    {
        public bool CanHandle(Type type)
        {
            return type.GetInterfaces().Union([type])
                   .Any(a => a == typeof(IList) ||
                           (a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)));

        }

        public IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            return new ItemListEditor()
            {
                Host = host
            };
        }
    }
}
