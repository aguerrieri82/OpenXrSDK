﻿namespace UI.Binding
{
    public interface IProperty
    {
        object? Value { get; set; }

        Type Type { get; }

        string? Name { get; }

        event EventHandler Changed;
    }

    public interface INameEdit
    {
        string? Name { get; set; }
    }

    public interface IProperty<T> : IProperty
    {
        new T Value { get; set; }

        Type IProperty.Type => typeof(T);

        object? IProperty.Value
        {
            get => Value;
            set => Value = (T)value!;
        }

    }
}
