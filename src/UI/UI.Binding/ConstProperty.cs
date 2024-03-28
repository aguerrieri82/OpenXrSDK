namespace UI.Binding
{
    public struct ConstProperty<T> : IProperty<T>
    {
        private readonly T _value;

        public ConstProperty(T value, string name)
        {
            _value = value;
            Name = name;
        }

        public T Value
        {
            get => _value;
            set => throw new NotSupportedException();
        }


        public string Name { get; }

        public event EventHandler? Changed;

    }
}
