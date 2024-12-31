using UI.Binding;

namespace XrEditor
{
    public interface IPropertyEditor
    {
        object Value { get; set; }

        Type ValueType { get; }

        IProperty? Binding { get; set; }

        void NotifyEditValueChanged();

        void NotifyBindValueChanged();


        event Action<IPropertyEditor>? ValueChanged;
    }
}
