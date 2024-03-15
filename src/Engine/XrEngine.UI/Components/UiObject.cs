using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Helpers;

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
        public UiProperty(string name, Type ownerType)
            : base(name, typeof(T), ownerType)
        {
        }
    }

    public abstract class UiObject
    {
        protected static Dictionary<Type, Dictionary<string, UiProperty>> _props = [];

        public static Dictionary<string, UiProperty> RegisterType<T>()
        {
            return RegisterType(typeof(T));
        }

        public static Dictionary<string, UiProperty> RegisterType(Type compType)
        {
            var props = new Dictionary<string, UiProperty>();

            foreach (var typeProp in compType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propType = typeof(UiProperty<>).MakeGenericType(typeProp.PropertyType);
                
                var prop = (UiProperty)Activator.CreateInstance(propType, typeProp.Name, compType)!;

                var defValue = typeProp.GetCustomAttribute<DefaultValueAttribute>();

                if (defValue != null)
                {
                    if (!UiTypeConverter.TryConvert(defValue.Value, typeProp.PropertyType, out prop.DefaultValue))
                        throw new InvalidCastException();
                }

                props[typeProp.Name] = prop;
            }

            _props[compType] = props;

            return props;
        }

        public static UiProperty<T> GetProperty<T>(string name, Type type)
        {
            return (UiProperty<T>)GetProperty(name, type);
        }

        public static UiProperty GetProperty(string name, Type type)
        {
            if (!_props.TryGetValue(type, out var props))
                props = RegisterType(type);
            return props[name];
        }

        protected Dictionary<UiProperty, object?>? _values;

        protected virtual void OnPropertyChanged(string propName, object? value, object? oldValue)
        {

        }

        public virtual void SetValue<T>(string propName, T? value)
        {
            var prop = GetProperty<T>(propName, GetType());

            if (value != null && !prop.Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException();
            
            _values ??= [];

            var oldValue = GetValue<T>(propName);

            if (!Equals(value, oldValue))
                OnPropertyChanged(propName, value, oldValue);

            _values[prop] = value;
        }

        public virtual T? GetValue<T>(string propName)
        {
            var prop = GetProperty<T>(propName, GetType());

            if (_values != null && _values.TryGetValue(prop, out var value))
                return (T?)value;
            return (T?)prop.DefaultValue;
        }
    }
}
