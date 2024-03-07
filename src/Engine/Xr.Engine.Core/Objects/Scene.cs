

namespace Xr.Engine
{
    public class Scene : Group3D, IObjectChangeListener
    {
        protected Camera? _activeCamera;
        protected List<IObjectChangeListener> _changeListener = [];
        protected readonly LayerManager _layers;
        protected EngineApp? _app;
        protected UpdateHistory _history;
        protected Canvas3D _gizmos;

        public Scene()
        {
            _layers = new LayerManager(this);
            _history = new UpdateHistory(this);
            _scene = this;
            _gizmos = new Canvas3D();

            AddChild(_gizmos.Content); 

            this.AddLayer<TypeLayer<Light>>();
            this.AddLayer<TypeLayer<Camera>>();
            this.AddLayer<TypeLayer<Object3D>>();
        }

        public override void Update(RenderContext ctx)
        {
            _gizmos.Clear();
            base.Update(ctx);
            _gizmos.Flush();
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
            if (change.Type != ObjectChangeType.Transform)
            {
                Version++;

                ((IObjectChangeListener)_layers).NotifyChanged(object3D, change);
            }

            if (_app != null)
            {
                foreach (var listener in _changeListener)
                    listener.NotifyChanged(object3D, change);

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

        public Canvas3D Gizmos => _gizmos;

        public UpdateHistory History => _history;

        public IList<IObjectChangeListener> ChangeListeners => _changeListener;

        public LayerManager Layers => _layers;

        public EngineApp? App => _app;

        public static Scene? Current { get; internal set; }

    }
}
