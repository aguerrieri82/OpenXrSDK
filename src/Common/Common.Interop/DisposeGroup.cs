namespace Common.Interop
{
    public class DisposeGroup : IDisposable
    {
        protected HashSet<IDisposable>? _items;

        public void Add(IDisposable item)
        {
            _items ??= [];
            _items.Add(item);
        }

        public void Dispose()
        {
            if (_items == null)
                return;
            foreach (var item in _items)
                item.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
