﻿
namespace OpenXr.Engine
{
    public class Scene : Group, IObjectChangeListener
    {
        protected Camera? _activeCamera;
        protected List<IObjectChangeListener> _changeListener = [];
        protected readonly LayerManager _layers;
        protected EngineApp? _app;

        public Scene()
        {
            _layers = new LayerManager(this);

            this.AddLayer<TypeLayer<Light>>();
            this.AddLayer<TypeLayer<Camera>>();
            this.AddLayer<TypeLayer<Object3D>>();
        }

        internal void Attach(EngineApp app)
        {
            _app = app;

            foreach (var obj in this.TypeLayerContent<Object3D>())
                NotifyChanged(obj, ObjectChangeType.SceneAdd);
        }

        public void Render(RenderContext ctx)
        {
            Update(ctx);
        }

        public void NotifyChanged(Object3D object3D, ObjectChange change)
        {
            Version++;

            ((IObjectChangeListener)_layers).NotifyChanged(object3D, change);

            if (_app != null)
            {
                foreach (var listener in _app.ChangeListeners)
                    listener.NotifyChanged(object3D, change);
            }
        }



        public Camera? ActiveCamera
        {
            get => _activeCamera;
            set
            {
                if (_activeCamera == value)
                    return;

                _activeCamera = value;

                if (_activeCamera != null && _activeCamera.Scene != this)
                    AddChild(_activeCamera);
            }
        }

        public IList<IObjectChangeListener> ChangeListeners => _changeListener;

        public LayerManager Layers => _layers;

        public static Scene? Current { get; internal set; }
    }
}