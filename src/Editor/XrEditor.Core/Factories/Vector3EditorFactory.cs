using System.Numerics;
using XrEngine;
using ValueType = XrEngine.ValueType;

namespace XrEditor
{
    public readonly struct Vector3EditorFactory : IPropertyEditorFactory
    {
        public readonly bool CanHandle(Type type)
        {
            return type == typeof(Vector3);
        }

        public readonly IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes, object? host)
        {
            var valueType = attributes.OfType<ValueTypeAttribute>().FirstOrDefault()?.Type ?? ValueType.None;

            var editor = valueType == ValueType.Direction ? new DirectionEditor() : new Vector3Editor();

            editor.SetAttributes(attributes);

            return editor;
        }
    }
}
