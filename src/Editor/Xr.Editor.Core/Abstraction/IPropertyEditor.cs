namespace Xr.Editor
{
    public interface IPropertyEditor
    {
        object Value { get; set; }

        void NotifyValueChanged();
    }
}
