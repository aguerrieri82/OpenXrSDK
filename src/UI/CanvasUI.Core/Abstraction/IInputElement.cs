namespace CanvasUI
{
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
    }
}
