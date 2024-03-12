using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Components
{
    [Flags]
    public enum UiPropertyFlags
    {
        None,
        Layout = 0x1
    }

    public class UiProperty
    {
        public UiProperty(string name, Type type, Type ownerType)
        {
            Name = name;
            OwnerType = ownerType;
            Type = type;
        }

        public string Name;

        public Type Type;

        public object? DefaultValue;

        public Type OwnerType;

        public UiPropertyFlags Flags;
    }

    public class UiProperty<T> : UiProperty
    {
        public UiProperty(string name,  Type ownerType)
            : base(name, typeof(T), ownerType)
        {
        }
    }

    public abstract class UiObject
    {
        protected static Dictionary<string, UiProperty> _props = [];

        protected Dictionary<UiProperty, object?>? _values;

        protected static UiProperty<T> CreateProp<T>(string name, Type ownerType, T? defaultValue = default) 
        {
            var result = new UiProperty<T>(name, ownerType)
            {
                DefaultValue = defaultValue
            };

            _props[name] = result;

            return result;
        }

        protected virtual void OnPropertyChanged<T>(UiProperty<T> prop, T? value, T? oldValue)
        {

        }

        public void SetValue<T>(UiProperty<T> prop, T? value)
        {
            if (value != null && !prop.Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException();
            
            _values ??= [];

            var oldValue = GetValue(prop);

            if (!Equals(value, oldValue))
                OnPropertyChanged(prop, value, oldValue);

            _values[prop] = value;
        }

        public T? GetValue<T>(UiProperty<T> prop)
        {
            if (_values != null && _values.TryGetValue(prop, out var value))
                return (T?)value;
            return (T?)prop.DefaultValue;
        }
    }
}
