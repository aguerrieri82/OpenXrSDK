using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xr.Math;

namespace Xr.Engine
{
    public class TriangleMesh : Object3D, IVertexSource<VertexData, uint>, ILocalBounds
    {
        readonly ObservableCollection<Material> _materials;
        private Geometry3D? _geometry;

        public TriangleMesh()
        {
            _materials = [];
            _materials.CollectionChanged += OnMaterialsChanged;
        }

        public override T? Feature<T>() where T : class
        {
            if (typeof(T) == typeof(Geometry3D))
                return (T?)(object?)Geometry;
            return base.Feature<T>();
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

        public override void UpdateBounds()
        {
            if (Geometry != null)
                _worldBounds = Geometry.Bounds.Transform(WorldMatrix);
            _boundsDirty = false;
        }

        public TriangleMesh(Geometry3D geometry, Material? material = null)
            : this()
        {
            Geometry = geometry;

            if (material != null)
                Materials.Add(material);
        }

        public override void Update(RenderContext ctx)
        {
            _materials.Update(ctx);

            Geometry?.Update(ctx);

            base.Update(ctx);
        }

        public IList<Material> Materials => _materials;

        public Geometry3D? Geometry
        {
            get => _geometry;
            set
            {
                if (_geometry == value)
                    return;
                _geometry = value;
                _boundsDirty = true;
                _geometry?.EnsureId();
                NotifyChanged(ObjectChangeType.Geometry);
            }
        }

        public Bounds3 LocalBounds => _geometry!.Bounds;


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
