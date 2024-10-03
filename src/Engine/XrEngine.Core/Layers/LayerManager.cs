namespace XrEngine
{
    public class LayerManager : IObjectChangeListener
    {
        readonly Scene3D _scene;
        readonly HashSet<string> _layersContent = [];
        readonly List<ILayer3D> _layers = [];
        long _version;
        public LayerManager(Scene3D scene)
        {
            _scene = scene;
        }

        public T Add<T>(T layer) where T : ILayer3D
        {
            if (_layers.Contains(layer))
                return layer;

            _layers.Add(layer);

            layer.Attach(this);

            foreach (var obj in layer.Content)
                NotifyObjectAdded(layer, obj);

            foreach (var obj in _scene!.Descendants<Object3D>())
                layer.NotifyChanged(obj, ObjectChangeType.SceneAdd);

            _version++;

            return layer;
        }

        public IEnumerable<T> OfType<T>() where T : ILayer3D
        {
            return _layers.OfType<T>();
        }

        public void Remove(ILayer3D layer)
        {
            foreach (var obj in layer.Content)
                NotifyObjectRemoved(layer, obj);

            _layers.Remove(layer);

            layer.Detach();

            _version++;
        }

        void IObjectChangeListener.NotifyChanged(Object3D object3D, ObjectChange change)
        {
            foreach (var layer in _layers.Where(a => a.IsEnabled))
                layer.NotifyChanged(object3D, change);
        }

        protected static string Hash(ILayer3D layer, ILayer3DItem obj)
        {
            return $"{layer.Id}|{obj.Id}";
        }

        internal protected void NotifyObjectAdded(ILayer3D layer, ILayer3DItem obj)
        {
            _layersContent.Add(Hash(layer, obj));
        }

        internal protected void NotifyObjectRemoved(ILayer3D layer, ILayer3DItem obj)
        {
            _layersContent.Remove(Hash(layer, obj));
        }

        public bool LayerContains(ILayer3D layer, ILayer3DItem obj)
        {
            return _layersContent.Contains(Hash(layer, obj));
        }

        public Scene3D Scene => _scene;

        public IReadOnlyList<ILayer3D> Layers => _layers;

        public long Version => _version;
    }
}
