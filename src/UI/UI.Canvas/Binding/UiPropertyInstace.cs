using UI.Binding;

namespace CanvasUI
{
    public class UiPropertyInstance<T> : IProperty<T>, IDisposable
    {
        readonly UiObject _obj;
        readonly UiProperty<T> _prop;

        public UiPropertyInstance(UiObject obj, string name)
            : this(obj, (UiProperty<T>)UiObject.GetProperty(name, obj.GetType()))
        {
        }

        public UiPropertyInstance(UiObject obj, UiProperty<T> prop)
        {
            _obj = obj;
            _prop = prop;
            _obj.PropertyChanged += OnPropertyChanged;
        }


        public void Dispose()
        {
            _obj.PropertyChanged -= OnPropertyChanged;
            GC.SuppressFinalize(this);
        }

        public T Value
        {
            get => _obj.GetValue<T>(_prop.Name)!;
            set
            {
                _obj.SetValue(_prop.Name, value);
            }
        }

        private void OnPropertyChanged(UiObject owner, string propName, object? value, object? oldValue)
        {
            if (propName == _prop.Name)
                Changed?.Invoke(this, EventArgs.Empty);
        }

        public string Name => _prop.Name;

        public event EventHandler? Changed;

    }
}
