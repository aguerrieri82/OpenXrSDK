namespace XrEngine.Layers
{
    public abstract class BaseLayer<T> : ILayer3D where T : ILayer3DItem
    {
        protected readonly ObjectId _id;
        protected readonly HashSet<T> _content = [];
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

        public void NotifyChanged(Object3D object3D, ObjectChange change)
        {
            if (!IsEnabled)
                return;

            if (object3D is Group3D group && change.IsAny(ObjectChangeType.SceneAdd, ObjectChangeType.SceneRemove))
            {
                foreach (var child in group.DescendantsOrSelf().OfType<T>())
                {
                    if (child is Object3D child3D)
                        NotifyChangedWork(child3D, change.Type);
                }
            }
            else
                NotifyChangedWork(object3D, change);
        }

        protected virtual void NotifyChangedWork(Object3D object3D, ObjectChange change)
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

        public bool IsVisible { get; set; }

        public ObjectId Id => _id;

        public long Version => _version;

        public string Name { get; set; }

        public IEnumerable<ILayer3DItem> Content => (IEnumerable<ILayer3DItem>)_content;

    }
}
