

namespace CanvasUI
{
    public enum UiStyleMode
    {
        Value,
        Default,
        Inherit,
        NotSet,
        Auto
    }

    public interface IStyleValue
    {
        UiStyleMode Mode { get; }

        object? Value { get; }
    }

    public struct StyleValue<T> : IStyleValue
    {
        public static readonly StyleValue<T> Inherit = new() { Mode = UiStyleMode.Inherit };

        public static readonly StyleValue<T> Default = new() { Mode = UiStyleMode.Default };

        public static readonly StyleValue<T> NotSet = new() { Mode = UiStyleMode.NotSet };

        public static implicit operator StyleValue<T>(T? value)
        {
            return new StyleValue<T>() { Value = value };
        }

        public static implicit operator StyleValue<T>(UiStyleMode mode)
        {
            return new StyleValue<T>() { Mode = mode };
        }

        public static implicit operator T?(StyleValue<T> value)
        {
            return value.Value;
        }

        readonly UiStyleMode IStyleValue.Mode => Mode;

        readonly object? IStyleValue.Value => Value;

        public readonly bool HasValue => Mode == UiStyleMode.Value && Value != null;


        public T? Value;

        public UiStyleMode Mode;

    }
}
