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

    public interface IUiStyleValue
    {
        UiStyleMode Mode { get; }

        object? Value { get; }  
    }

    public struct UiStyleValue<T> : IUiStyleValue
    {


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

        readonly UiStyleMode IUiStyleValue.Mode => Mode;

        readonly object? IUiStyleValue.Value => Value;


        public T? Value;

        public UiStyleMode Mode;

    }
}
