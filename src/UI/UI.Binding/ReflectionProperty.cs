

using System.Reflection;

namespace UI.Binding
{
    public class ReflectionProperty : IProperty
    {
        protected readonly PropertyInfo _property;
        protected readonly object _object;

        public ReflectionProperty(PropertyInfo property, object obj)
        {
            _property = property;
            _object = obj;

            if (_object is  System.ComponentModel.INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == _property.Name)
                        OnChanged();
                };
            }
        }

        public object? Value
        {
            get => _property.GetValue(_object, null);
            set
            {
                if (Equals(value, Value))
                    return;

                _property.SetValue(_object, value);

                OnChanged();
            }
        }

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public Type Type => _property.PropertyType;

        public string? Name => _property.Name;

        public event EventHandler? Changed;
    }


    public class ReflectionProperty<T> : ReflectionProperty, IProperty<T>
    {
        public ReflectionProperty(PropertyInfo property, object obj) : base(property, obj)
        {
        }

        T IProperty<T>.Value
        {
            get => (T)Value!;
            set => Value = value;
        }
    }
}
