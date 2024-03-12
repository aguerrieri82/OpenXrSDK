using XrEngine.UI.Components;

namespace XrEngine.UI
{
    public enum UiStyleMode
    {
        Value,
        Default,
        Inherit,
        NotSet,
        Auto
    }


    public struct UiStyleValue<T>
    {
        internal UiProperty<UiStyleValue<T>> _property;

        public T? Value;

        public UiStyleMode Mode;

        public readonly T? ActualValue(UiComponent component)
        {
            
            if (Mode == UiStyleMode.Value)
                return Value;

            if (Mode == UiStyleMode.Inherit && component.Parent != null)
                return component.Parent.Style.GetStyleValue(_property).ActualValue(component.Parent);

            return default;
        }

        public static readonly UiStyleValue<T> Inherit = new() { Mode = UiStyleMode.Inherit };

        public static readonly UiStyleValue<T> Default = new() { Mode = UiStyleMode.Default };

        public static readonly UiStyleValue<T> NotSet = new() { Mode = UiStyleMode.NotSet };

        public static implicit operator UiStyleValue<T>(T? value)
        {
            return new UiStyleValue<T>() { Value = value };
        }

        public static implicit operator UiStyleValue<T>(UiStyleMode mode)
        {
            return new UiStyleValue<T>() { Mode = mode };
        }

        public static implicit operator T?(UiStyleValue<T> value)
        {
            return value.Value;
        }
    }
}
