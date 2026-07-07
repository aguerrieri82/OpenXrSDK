using System.Collections.ObjectModel;
using System.Collections.Specialized;
using XrEngine;


namespace XrEditor.Services
{
    public class SelectionManager
    {
        protected BulkObservableCollection<INode> _items = [];
        protected bool _isChanged;
        protected int _update;
        protected readonly IMainDispatcher _mainDispatcher;

        public SelectionManager()
        {
            _items.CollectionChanged += OnChanged;
            _mainDispatcher = Context.Require<IMainDispatcher>();
        }

        protected virtual void NotifyChanged()
        {
            _isChanged = false;
            Changed?.Invoke(_items.ToArray().AsReadOnly());
        }

        private async void OnChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_update > 0)
                _isChanged = true;
            else
                NotifyChanged();

            await EngineApp.MainThread;

            var oldItems = e.OldItems?.Cast<INode>().Select(a => a.Value).OfType<Object3D>();
            var newItems = e.NewItems?.Cast<INode>().Select(a => a.Value).OfType<Object3D>();

            var isSel = e.Action == NotifyCollectionChangedAction.Add;

            var valid = e.Action == NotifyCollectionChangedAction.Add ||
                        e.Action == NotifyCollectionChangedAction.Remove;

            var curItems = isSel ? newItems : oldItems;

            if (curItems != null && valid)
            {
                foreach (var item in curItems)
                {
                    foreach (var handler in item.Components<ISelectionHandler>())
                        handler.OnSelected(item, isSel);
                }
            }
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

        public async void Clear()
        {
            if (_items.Count == 0)
                return;

            await _mainDispatcher.Switch;

            for (var i = _items.Count - 1; i >= 0; i--)
                _items.RemoveAt(i);
        }

        public async void Set(params INode[] items)
        {
            if (items.SequenceEqual(_items))
                return;

            BeginUpdate();

            Clear();

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
