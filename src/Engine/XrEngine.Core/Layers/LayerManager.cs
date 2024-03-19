namespace XrEngine
{
    public class LayerManager : IObjectChangeListener
    {
        readonly Scene3D _scene;
        readonly HashSet<string> _layersContent = [];
        readonly List<ILayer3D> _layers = [];

        public LayerManager(Scene3D scene)
        {
            _scene = scene;
        }

        public void Add(ILayer3D layer)
        {
            if (_layers.Contains(layer))
                return;

            _layers.Add(layer);

            layer.Attach(this);

            foreach (var obj in layer.Content)
                NotifyObjectAdded(layer, obj);

            foreach (var obj in _scene!.Descendants<Object3D>())
                layer.NotifyChanged(obj, ObjectChangeType.SceneAdd);
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
        }

        void IObjectChangeListener.NotifyChanged(Object3D object3D, ObjectChange change)
        {
            foreach (var layer in _layers)
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
    }
}
