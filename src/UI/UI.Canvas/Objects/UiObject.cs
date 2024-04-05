using System.Reflection;
using UI.Binding;

namespace CanvasUI
{

    public delegate void UiPropertyChangedHandler(UiObject owner, string propName, object? value, object? oldValue);

    public abstract class UiObject : IDisposable
    {
        #region PROPERTIES 

        protected static Dictionary<Type, Dictionary<string, UiProperty>> _props = [];

        public static Dictionary<string, UiProperty> RegisterType<T>()
        {
            return RegisterType(typeof(T));
        }

        public static Dictionary<string, UiProperty> RegisterType(Type compType)
        {
            if (_props.TryGetValue(compType, out var props))
                return props;

            props = [];

            foreach (var typeProp in compType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!typeProp.CanRead || !typeProp.CanWrite)
                    continue;

                var propDesc = typeProp.GetCustomAttribute<UiPropertyAttribute>();
                if (propDesc == null)
                    continue;

                var propType = typeof(UiProperty<>).MakeGenericType(typeProp.PropertyType);

                var prop = (UiProperty)Activator.CreateInstance(propType, typeProp.Name, compType)!;

                prop.Flags = propDesc.Flags;

                if (!TypeConverter.TryConvert(propDesc.DefaultValue, typeProp.PropertyType, out prop.DefaultValue))
                    throw new InvalidCastException();

                props[typeProp.Name] = prop;
            }

            var curBase = compType.BaseType;

            while (curBase != null && curBase != typeof(object))
            {
                var baseProps = RegisterType(curBase);

                foreach (var prop in baseProps)
                    props[prop.Key] = prop.Value;

                curBase = curBase.BaseType;
            }

            _props[compType] = props;

            return props;
        }

        public static UiProperty<TValue> GetProperty<TValue>(string name, Type ownerType)
        {
            return (UiProperty<TValue>)GetProperty(name, ownerType);
        }

        public static UiProperty GetProperty(string name, Type ownerType)
        {
            if (!_props.TryGetValue(ownerType, out var props))
                props = RegisterType(ownerType);
            return props[name];
        }

        #endregion

        protected Dictionary<UiProperty, object?>? _values;
        protected List<Binding>? _bindings = null;

        protected virtual void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            PropertyChanged?.Invoke(this, propName, value, oldValue);
        }

        public virtual void SetValue<T>(string propName, T? value)
        {
            var prop = GetProperty<T>(propName, GetType());

            if (value != null && !prop.Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException();

            _values ??= [];

            var oldValue = GetValue<T>(propName);

            _values[prop] = value;

            if (!Equals(value, oldValue))
                OnPropertyChanged(propName, value, oldValue);

        }

        public virtual T? GetValue<T>(string propName)
        {
            var prop = GetProperty<T>(propName, GetType());

            if (_values != null && _values.TryGetValue(prop, out var value))
                return (T?)value;

            if (prop.DefaultValue != null)
                return (T?)prop.DefaultValue;

            return default;
        }

        public Binding Bind<TValue>(string propName, IProperty<TValue> other, BindingMode mode)
        {
            _bindings ??= [];

            var result = new Binding(other, new UiPropertyInstance<TValue>(this, propName), mode);
            _bindings.Add(result);
            return result;
        }

        public virtual void Dispose()
        {
            if (_bindings != null)
            {
                foreach (var binding in _bindings)
                    binding.Dispose();
            }

            _bindings = null;
            _props.Clear();

            GC.SuppressFinalize(this);
        }


        public event UiPropertyChangedHandler? PropertyChanged;
    }
}
