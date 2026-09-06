using UI.Binding;

namespace XrEditor
{
    public interface IPropertyEditor
    {
        object Value { get; set; }

        Type ValueType { get; }

        IProperty? Binding { get; set; }

        void SetAttributes(IEnumerable<Attribute> attributes);

        void NotifyEditValueChanged();

        void NotifyBindValueChanged();

        event Action<IPropertyEditor>? ValueChanged;

        object? Host { get; set; }
    }
}
