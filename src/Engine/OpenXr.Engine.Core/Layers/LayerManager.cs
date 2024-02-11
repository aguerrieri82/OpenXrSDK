using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class LayerManager : IObjectChangeListener
    {
        readonly Scene _scene;
        readonly HashSet<string> _layersContent = [];
        readonly List<ILayer> _layers = [];

        public LayerManager(Scene scene) 
        {
            _scene = scene;
        }

        public void Add(ILayer layer)
        {
            if (_layers.Contains(layer))
                return;

            _layers.Add(layer);

            layer.Attach(this);

            foreach (var obj in layer.Content)
                NotifyObjectAdded(layer, obj);
        }

        public IEnumerable<T> OfType<T>() where T : ILayer
        {
            return _layers.OfType<T>(); 
        }

        public void Remove(ILayer layer)
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

        protected static string Hash(ILayer layer, ILayerObject obj)
        {
            return $"{layer.Id}|{obj.Id}";  
        }

        internal protected void NotifyObjectAdded(ILayer layer, ILayerObject obj)
        {
            _layersContent.Add(Hash(layer, obj));
        }

        internal protected void NotifyObjectRemoved(ILayer layer, ILayerObject obj)
        {
            _layersContent.Remove(Hash(layer, obj));
        }

        public bool LayerContains(ILayer layer, ILayerObject obj)
        {
            return _layersContent.Contains(Hash(layer, obj));
        }
    }
}
