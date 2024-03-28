namespace CanvasUI
{

    public delegate void InputValueChangedHandler<T>(IInputElement<T> sender, T value, T oldValue);

    public interface IInputElement
    {
        object? Value { get; set; }

        Type ValueType { get; }
    }

    public interface IInputElement<T> : IInputElement
    {
        object? IInputElement.Value
        {
            get => Value;
            set => Value = (T)value!;
        }

        Type IInputElement.ValueType => typeof(T);

        new T Value { get; set; }


        event InputValueChangedHandler<T> ValueChanged;
    }
}
