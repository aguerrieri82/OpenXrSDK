namespace CanvasUI
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

        public T Get()
        {
            return _getter();
        }

        public void Set(T value)
        {
            _setter(value);
        }

        public string Name { get; }


        public event EventHandler? Changed;

    }
}
