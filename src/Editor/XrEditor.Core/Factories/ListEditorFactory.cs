using System.Collections;
using XrEngine;

namespace XrEditor
{
    public readonly struct ListEditorFactory : IPropertyEditorFactory
    {
        public readonly bool CanHandle(Type type)
        {
            return type.GetInterfaces().Union([type])
                   .Any(a => a == typeof(IList) ||
                           (a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)));

        }

        public readonly IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            var editable = attributes.OfType<EditableAttribute>().FirstOrDefault();
            if (editable == null)
                return null;

            return new ItemListEditor()
            {
                Host = host
            };
        }
    }
}
