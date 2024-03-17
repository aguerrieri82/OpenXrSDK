using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public delegate void PropertyChangedHandler<T>(T? obj, IProperty property, object? value, object? oldValue);

    public class Binder<T>
    {
        public Binder(T value) 
        {
            Value = value;
        }

        public IProperty<TVal> Prop<TVal>(Expression<Func<T, TVal>> exp)
        {
            var getter = exp.Compile();

            return new SimpleProperty<TVal>(() => getter(Value), null);
        }

        public event PropertyChangedHandler<T>? PropertyChanged;

        public T Value;
    }
}
