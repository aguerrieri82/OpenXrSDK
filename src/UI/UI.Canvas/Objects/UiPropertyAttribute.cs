namespace CanvasUI
{
    public class UiPropertyAttribute : Attribute
    {
        public UiPropertyAttribute(object? defaultValue = null, UiPropertyFlags flags = UiPropertyFlags.None)
        {
            DefaultValue = defaultValue;
            Flags = flags;
        }

        public object? DefaultValue { get; }

        public UiPropertyFlags Flags { get; }
    }
}
