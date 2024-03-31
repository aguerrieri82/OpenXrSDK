using System.Collections.ObjectModel;
using System.Collections.Specialized;
using XrMath;

namespace XrEngine
{
    public class TriangleMesh : Object3D, IVertexSource<VertexData, uint>, ILocalBounds
    {
        readonly ObservableCollection<Material> _materials;
        private Geometry3D? _geometry;

        public TriangleMesh()
        {
            _materials = [];
            _materials.CollectionChanged += OnMaterialsChanged;
            BoundUpdateMode = UpdateMode.Automatic;
        }

        public TriangleMesh(Geometry3D geometry, Material? material = null)
            : this()
        {
            Geometry = geometry;

            if (material != null)
                Materials.Add(material);
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);

            container.WriteRef(nameof(Geometry), Geometry);
            container.WriteRefArray(nameof(Materials), _materials);
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);

            Geometry = container.Read<Geometry3D>(nameof(Geometry));

            container.ReadArray(ctx, nameof(Materials), _materials, _materials.Add, a => _materials.Remove(a));
        }

        public override T? Feature<T>() where T : class
        {
            if (typeof(T) == typeof(Geometry3D))
                return (T?)(object?)Geometry;
            return base.Feature<T>();
        }


        public override void UpdateBounds(bool force = false)
        {
            if (Geometry != null)
                _worldBounds = Geometry.Bounds.Transform(WorldMatrix);

            _boundsDirty = false;
        }


        public override void Update(RenderContext ctx)
        {
            _materials.Update(ctx);

            Geometry?.Update(ctx);

            base.Update(ctx);
        }

        private void OnMaterialsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in e.OldItems!.Cast<Material>())
                    item.Detach();
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.Cast<Material>())
                {
                    item.EnsureId();
                    item.Attach(this);
                }
            }

            NotifyChanged(ObjectChangeType.Render);
        }

        public Geometry3D? Geometry
        {
            get => _geometry;
            set
            {
                if (_geometry == value)
                    return;
                _geometry = value;
                _geometry?.EnsureId();
                NotifyChanged(ObjectChangeType.Geometry);
            }
        }

        public IList<Material> Materials => _materials;

        public Bounds3 LocalBounds => _geometry!.Bounds;

        public UpdateMode BoundUpdateMode { get; set; }

        #region IVertexSource

        EngineObject IVertexSource.Object => _geometry!;

        VertexComponent IVertexSource.ActiveComponents => _geometry?.ActiveComponents ?? VertexComponent.None;

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Triangle;

        uint[] IVertexSource<VertexData, uint>.Indices => _geometry?.Indices ?? [];

        VertexData[] IVertexSource<VertexData, uint>.Vertices => _geometry?.Vertices ?? [];

        IList<Material> IVertexSource.Materials => _materials;

        #endregion
    }
}
