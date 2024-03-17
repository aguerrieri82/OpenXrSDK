using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public interface IProperty
    {
        object? Get();

        void Set(object? value);    

        Type Type { get; }

        string? Name { get; }   

        event EventHandler Changed; 
    }

    public interface IProperty<T> : IProperty
    {
        new T Get();

        void Set(T value);

        Type IProperty.Type => typeof(T);

        object? IProperty.Get() => Get();

        void IProperty.Set(object? value) => Set((T)value!);

    }
}
