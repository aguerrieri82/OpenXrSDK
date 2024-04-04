using UI.Binding;

namespace XrEditor
{
    public interface IPropertyEditor
    {
        object Value { get; set; }

        Type ValueType { get; }

        IProperty? Binding { get; set; }

        void NotifyValueChanged();


        event Action<IPropertyEditor>? ValueChanged;
    }
}
