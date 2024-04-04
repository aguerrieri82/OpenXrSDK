namespace UI.Binding
{
    public struct SimpleProperty<T> : IProperty<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private T _lastValue;

        public SimpleProperty(Func<T> getter, Action<T> setter, string name)
        {
            _getter = getter;
            _setter = setter;
            _lastValue = getter();
            Name = name;
        }

        public T Value
        {
            get 
            {
                _lastValue = _getter();
                return _lastValue;
            }

            set 
            {
                if (Equals(value, Value))
                    return;

                _setter(value);

                _lastValue = value;

                Changed?.Invoke(this, EventArgs.Empty);
            }
        }


        public string Name { get; }


        public event EventHandler? Changed;

    }
}
