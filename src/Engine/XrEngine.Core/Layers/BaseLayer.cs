namespace XrEngine.Layers
{
    public abstract class BaseLayer<T> : ILayer3D where T : ILayer3DItem
    {
        protected readonly ObjectId _id;
        protected readonly List<T> _content = [];
        protected LayerManager? _manager;
        protected long _version;

        private bool _isEnabled;

        public BaseLayer()
        {
            _id = ObjectId.New();
            _isEnabled = true;
            Name = GetType().Name;
        }

        public void Attach(LayerManager manager)
        {
            _manager = manager;
        }

        public virtual void Detach()
        {
            _manager = null;
        }

        public void NotifyChanged(Object3D sender, ObjectChange change)
        {
            if (!IsEnabled)
                return;

            if (sender is Group3D group && change.IsAny(ObjectChangeType.Scene))
            {
                foreach (var child in group.DescendantsOrSelf())
                    NotifyChangedWork(child, change.Type);
            }
            else
                NotifyChangedWork(sender, change);
        }

        protected virtual void NotifyChangedWork(Object3D sender, ObjectChange change)
        {

        }

        protected virtual void OnEnabledChanged()
        {
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                OnEnabledChanged();
            }
        }

        IEnumerable<ILayer3DItem> ILayer3D.Content => (IEnumerable<ILayer3DItem>)_content;

        public bool IsVisible { get; set; }

        public string Name { get; set; }

        public ObjectId Id => _id;

        public long Version => _version;

        public IReadOnlyCollection<T> Content => _content;

    }
}
