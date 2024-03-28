namespace UI.Binding
{
    public struct SimpleProperty<T> : IProperty<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        public SimpleProperty(Func<T> getter, Action<T> setter, string name)
        {
            _getter = getter;
            _setter = setter;
            Name = name;
        }

        public T Value
        {
            get => _getter();

            set 
            {
                if (Equals(value, Value))
                    return;

                _setter(value);

                Changed?.Invoke(this, EventArgs.Empty);
            }
        }


        public string Name { get; }


        public event EventHandler? Changed;

    }
}
