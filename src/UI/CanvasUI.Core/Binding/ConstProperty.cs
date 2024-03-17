namespace CanvasUI
{
    public struct ConstProperty<T> : IProperty<T>
    {
        private readonly T _value;

        public ConstProperty(T value, string name)
        {
            _value = value;
            Name = name;
        }

        public T Get()
        {
            return _value;
        }

        public void Set(T value)
        {
            throw new NotSupportedException();
        }

        public string Name { get; }

        public event EventHandler? Changed;

    }
}
