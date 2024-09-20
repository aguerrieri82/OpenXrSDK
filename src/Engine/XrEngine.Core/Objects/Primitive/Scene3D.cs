

using XrEngine.Layers;

namespace XrEngine
{
    public class Scene3D : Group3D, IObjectChangeListener
    {
        protected Camera? _activeCamera;
        protected List<IObjectChangeListener> _changeListener = [];
        protected readonly LayerManager _layers;
        protected EngineApp? _app;
        protected UpdateHistory _history;
        protected Canvas3D _gizmos;
        protected IDrawGizmos[]? _drawGizmos;

        public Scene3D()
        {
            _layers = new LayerManager(this);
            _history = new UpdateHistory(this);
            _scene = this;
            _gizmos = new Canvas3D();
            
            this.AddLayer<TypeLayer<Light>>();
            this.AddLayer<TypeLayer<Camera>>();
            this.AddLayer<TypeLayer<Object3D>>();
            this.AddLayer<TypeLayer<Object3D>>();
            this.AddLayer<CastShadowsLayer>();

            this.AddLayer(new DetachedLayer() { Name = "Gizmos" }).Add(_gizmos.Content);
        }

        public void DrawGizmos()
        {
            if (_drawGizmos == null || _drawGizmos.Length == 0)
                return;

            _gizmos.Clear();
            
            foreach (var draw in _drawGizmos)
                draw.DrawGizmos(_gizmos);

            _gizmos.Flush();
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            _layers.Layers.OfType<IRenderUpdate>().Update(ctx);

            base.UpdateSelf(ctx);
        }

        protected void UpdateDrawGizmos()
        {
            _drawGizmos = this.DescendantsOrSelfComponents<IDrawGizmos>().ToArray();
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
            if (!change.IsAny(ObjectChangeType.Transform) &&
                change.Target is not Material && 
                (change.Target is not Light || !change.IsAny(ObjectChangeType.Render)))
            {
                Version++;
                
                UpdateDrawGizmos();

                ((IObjectChangeListener)_layers).NotifyChanged(object3D, change);
            }

            foreach (var listener in _changeListener)
                listener.NotifyChanged(object3D, change);

            if (_app != null)
            {
                foreach (var listener in _app.ChangeListeners)
                    listener.NotifyChanged(object3D, change);
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(ActiveCamera), ActiveCamera);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            ActiveCamera = container.Read<Camera>(nameof(ActiveCamera));
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

        public static Scene3D? Current { get; internal set; }
    }
}
