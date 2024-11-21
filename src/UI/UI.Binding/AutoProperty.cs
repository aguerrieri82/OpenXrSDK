using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Binding
{
    public class AutoProperty<T> : IProperty<T>, INameEdit
    {
        T _value;

        public T Value
        {
            get => _value;
            set 
            {
                if (Equals(value, _value))
                    return;
                _value = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public string? Name { get; set; }

        public event EventHandler? Changed;

        public static implicit operator T(AutoProperty<T> value)
        {
            return value._value;
        }
    }
}
