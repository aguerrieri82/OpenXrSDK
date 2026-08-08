using System.Collections.ObjectModel;
using System.Collections.Specialized;
using XrEngine.Objects;
using XrMath;

namespace XrEngine
{
    public class TriangleMesh : Object3D, IVertexSource<VertexData, uint>, ILocalBounds
    {
        protected readonly ObservableCollection<Material> _materials;
        protected Geometry3D? _geometry;

        protected Geometry3D? _originalGeometry;

        public TriangleMesh()
        {
            _materials = [];
            _materials.CollectionChanged += OnMaterialsChanged;
            BoundUpdateMode = UpdateMode.Automatic;
            Export = new(this);
            InstanceCount = 1;
        }

        public TriangleMesh(Geometry3D geometry, Material? material = null)
            : this()
        {
            Geometry = geometry;

            if (material != null)
                Materials.Add(material);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);

            if (Geometry != null)
                container.Write(nameof(Geometry), Geometry);

            container.WriteArray(nameof(Materials), _materials);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Geometry = container.Read(nameof(Geometry), Geometry);
            container.ReadArray(nameof(Materials), _materials, _materials.Add, a => _materials.Remove(a));
        }

        public override T? Feature<T>() where T : class
        {
            if (Geometry is T geo)
                return geo;
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
            base.Update(ctx);

            _materials.Update(ctx);

            Geometry?.Update(ctx);
        }

        private void OnMaterialsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems!.Cast<Material>())
                        item.Detach(this, false);

                    NotifyChanged(new ObjectChange(ChangeType.MateriaRemove, e.OldItems));
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.Cast<Material>())
                    item.Attach(this);

                NotifyChanged(new ObjectChange(ChangeType.MateriaAdd, e.NewItems));
            }
        }

        protected override void OnChanged(ObjectChange change)
        {
            if (change.Type == ChangeType.SceneRemove)
            {
                foreach (var material in _materials)
                    material.Detach(this);
            }

            if (change.Type == ChangeType.SceneAdd)
            {
                foreach (var material in _materials)
                    material.Attach(this);
            }

            base.OnChanged(change);
        }

        public void NotifyLoaded()
        {
            _geometry?.NotifyLoaded();
        }

        public void ReplaceGeometry(Geometry3D geometry)
        {
            _originalGeometry ??= _geometry;
            Geometry = geometry;
        }

        public Geometry3D? OriginalGeometry => _originalGeometry;

        public Geometry3D? Geometry
        {
            get => _geometry;
            set
            {
                if (_geometry == value)
                    return;

                if (_geometry != null)
                    _geometry.Detach(this);

                _geometry = value;

                if (_geometry != null)
                    _geometry.Attach(this);

                NotifyChanged(ChangeType.Geometry);
            }
        }

        public override void Dispose()
        {
            foreach (var material in Materials)
                material.Detach(this, true);

            Geometry?.Detach(this, true);

            Geometry = null;
            Materials.Clear();

            base.Dispose();
        }

        protected override void CloneWork(Object3D newObj, ObjectCloneFlags flags)
        {
            base.CloneWork(newObj, flags);

            var mesh = (TriangleMesh)newObj;

            var curGeo = _originalGeometry ?? _geometry;

            if ((flags & ObjectCloneFlags.CloneGeometry) != 0)
                mesh._geometry = curGeo?.Clone();
            else
                mesh._geometry = curGeo;

            foreach (var mat in _materials)
            {
                Material newMat;

                if ((flags & ObjectCloneFlags.CloneMaterials) != 0)
                    newMat = mat.Clone();
                else
                    newMat = mat;

                mesh._materials.Add(newMat);
            }
        }

        [Action]
        public void DoExport()
        {
            Export.Export();
        }

        public void NotifyBuffers(IBuffer<VertexData> vertices, IBuffer<uint>? indices)
        {
            VBuf = vertices;
            IBuf = indices;
        }

        public IBuffer<VertexData>? VBuf { get; internal set; }

        public IBuffer<uint>? IBuf { get; internal set; }

        public int RenderPriority { get; set; }

        public IList<Material> Materials => _materials;

        public Bounds3 LocalBounds => _geometry?.Bounds ?? Bounds3.Zero;

        public UpdateMode BoundUpdateMode { get; set; }

        public MeshExportInfo<TriangleMesh> Export { get; set; }

        public int InstanceCount { get; set; }

        #region IVertexSource

        EngineObject IVertexSource.Object => _geometry!;

        VertexComponent IVertexSource.ActiveComponents => _geometry?.ActiveComponents ?? VertexComponent.None;

        DrawPrimitive IVertexSource.Primitive => _geometry?.Primitive ?? DrawPrimitive.Triangle;

        uint[] IVertexSource<VertexData, uint>.Indices => _geometry?.Indices ?? [];

        VertexData[] IVertexSource<VertexData, uint>.Vertices => _geometry?.Vertices ?? [];

        IReadOnlyList<Material> IVertexSource.Materials => _materials;

        #endregion
    }
}
