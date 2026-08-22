namespace UI.Binding
{
    public struct SimpleProperty<T> : IProperty<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private T _lastValue;
        private readonly IEnumerable<Attribute>? _attributes;

        public SimpleProperty(Func<T> getter, Action<T> setter, string name, IEnumerable<Attribute>? attributes = null)
        {
            _getter = getter;
            _setter = setter;
            _lastValue = getter();
            _attributes = attributes;
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

        public readonly IEnumerable<Attribute> Attributes => _attributes ?? [];

        public string Name { get; }

        public event EventHandler? Changed;

    }
}
