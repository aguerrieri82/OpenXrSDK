namespace XrEditor
{
    public interface IPropertyEditor
    {
        object Value { get; set; }

        void NotifyValueChanged();
    }
}
