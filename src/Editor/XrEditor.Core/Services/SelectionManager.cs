using System.Collections.ObjectModel;

namespace XrEditor.Services
{
    public class SelectionManager
    {
        protected ObservableCollection<INode> _items = [];
        protected bool _isChanged;
        protected int _update;

        public SelectionManager()
        {
            _items.CollectionChanged += OnChanged;
        }

        protected virtual void NotifyChanged()
        {
            _isChanged = false;
            Changed?.Invoke(_items.AsReadOnly());
        }

        private void OnChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_update > 0)
            {
                _isChanged = true;
                return;
            }
            NotifyChanged();
        }

        public void BeginUpdate()
        {
            _update++;
        }

        public void EndUpdate()
        {
            _update--;
            if (_update == 0 && _isChanged)
                NotifyChanged();
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void Set(params INode[] items)
        {
            BeginUpdate();

            _items.Clear();
            foreach (var item in items)
                _items.Add(item);

            EndUpdate();
        }

        public bool IsSelected(INode value)
        {
            return _items.Contains(value);
        }


        public event Action<IReadOnlyCollection<INode>>? Changed;

        public ObservableCollection<INode> Items => _items;

    }
}
