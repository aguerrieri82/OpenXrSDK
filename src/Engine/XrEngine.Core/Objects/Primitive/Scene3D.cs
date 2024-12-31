namespace XrEngine
{
    public class Scene3D : Group3D, IObjectChangeListener
    {
        protected Camera? _activeCamera;
        protected EngineApp? _app;
        protected int _lockCount;
        protected readonly List<IObjectChangeListener> _changeListener;
        protected readonly LayerManager _layers;
        protected readonly Canvas3D _gizmos;
        protected readonly IList<IDrawGizmos> _drawGizmos;
        protected readonly RenderUpdateManager _updateManager;

        public Scene3D()
        {
            _drawGizmos = [];
            _changeListener = [];
            _layers = new LayerManager(this);
            _updateManager = new RenderUpdateManager(this);
            _scene = this;
            _gizmos = new Canvas3D();
            _changeListener.Add(_layers);

            this.AddLayer(new DetachedLayer()
            {
                Name = "Gizmos",
                Usage = DetachedLayerUsage.Gizmos

            }).Add(_gizmos.Content);
        }

        public void DrawGizmos()
        {
            if (_drawGizmos == null || _drawGizmos.Count == 0)
                return;

            _gizmos.Clear();

            foreach (var draw in _drawGizmos)
            {
                if (draw.IsEnabled)
                    draw.DrawGizmos(_gizmos);
            }

            _gizmos.Flush();
        }

        public override void Update(RenderContext ctx)
        {
            _updateManager.Update(ctx);
            //base.Update(ctx);
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            if (!ctx.UpdateOnlySelf)
                _layers.Layers.OfType<IRenderUpdate>().Update(ctx, false);

            base.UpdateSelf(ctx);
        }

        protected void UpdateDrawGizmos()
        {
            _drawGizmos.Clear();

            foreach (var obj in this.DescendantsOrSelf())
            {
                if (obj is IDrawGizmos draw)
                    _drawGizmos.Add(draw);

                foreach (var component in obj.Components<IComponent>().OfType<IDrawGizmos>())
                    _drawGizmos.Add(component);
            }
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

        public void NotifyChanged(Object3D sender, ObjectChange change)
        {
            //Debug.Assert(_app?.RenderThread == null || Thread.CurrentThread == _app.RenderThread);

            change.Target ??= sender;

            if (!change.IsAny(ObjectChangeType.Transform) &&
                !change.Targets<object>().All(a => a is Material) &&
                (change.Target is not Light || !change.IsAny(ObjectChangeType.Render)))
            {
                Version++;

                UpdateDrawGizmos();
            }

            if (change.IsAny(ObjectChangeType.Scene,
                             ObjectChangeType.MateriaAdd,
                             ObjectChangeType.MateriaRemove,
                             ObjectChangeType.Components))
            {
                ContentVersion++;
            }

            foreach (var listener in _changeListener)
                listener.NotifyChanged(sender, change);

            if (_app != null)
            {
                foreach (var listener in _app.ChangeListeners)
                    listener.NotifyChanged(sender, change);
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

        public void LockChanges()
        {
            _lockCount++;
        }

        public void UnlockChanges()
        {
            _lockCount--;
        }

        public void EnsureNotLocked()
        {
            if (_lockCount > 0)
                throw new InvalidOperationException("Scene changes are locked");
        }

        public long ContentVersion { get; protected set; }

        public Canvas3D Gizmos => _gizmos;

        public IList<IObjectChangeListener> ChangeListeners => _changeListener;

        public LayerManager Layers => _layers;

        public EngineApp? App => _app;

        public static Scene3D? Current { get; internal set; }
    }
}
