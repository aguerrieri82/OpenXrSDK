namespace XrEngine
{
    public class MutableArray<T>
    {
        readonly List<T> _list = [];

        public MutableArray()
        {
            Data = [];
        }

        public void Add(T value)
        {
            if (_list.Contains(value))
                return;

            _list.Add(value);

            if (Sort)
                _list.Sort();

            Data = _list.ToArray();
        }

        public static implicit operator T[](MutableArray<T> array) => array.Data;

        public bool Sort;

        public T[] Data;
    }
}
