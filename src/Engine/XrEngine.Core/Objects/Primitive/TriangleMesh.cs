using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Numerics;
using XrEngine.Objects;
using XrMath;

namespace XrEngine
{

    public enum MeshCompressionMode
    {
        Auto,
        Never,
        Always
    }


    public class TriangleMesh : Object3D, IVertexSource<VertexData, uint>, ILocalBounds, ICompressedVertexSource
    {
        protected readonly ObservableCollection<Material> _materials;
        protected Geometry3D? _geometry;
        protected Geometry3D? _originalGeometry;
        protected Bounds3 _localBounds;
        internal bool _localBoundsDirty;

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
            if (_geometry == null)
                return;

            if (_localBoundsDirty || force)
            {
                var skin = Feature<ISkinnedMesh>();

                if (skin != null)
                    _localBounds = skin.GetLocalBounds();
                else
                    _localBounds = _geometry.Bounds;

                _localBoundsDirty = false;
            }

            _worldBounds = _localBounds.Transform(WorldMatrix);

            _boundsDirty = false;
        }

        protected internal void InvalidateLocalBounds()
        {
            _localBoundsDirty = true;
            InvalidateBounds();
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

            if (change.IsAny(ChangeType.Material, ChangeType.Geometry))
                UpdateCompression();

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

        public override void Dispose()
        {
            foreach (var material in Materials)
                material.Detach(this, true);

            Geometry?.Detach(this, true);

            Geometry = null;
            Materials.Clear();

            base.Dispose();
        }

        protected override void CloneWork(EngineObject newObj, ObjectCloneFlags flags)
        {
            var mesh = (TriangleMesh)newObj;

            var curGeo = _originalGeometry ?? _geometry;

            if ((flags & ObjectCloneFlags.CloneGeometry) != 0)
                mesh._geometry = curGeo?.Clone(flags);
            else
                mesh._geometry = curGeo;

            foreach (var mat in _materials)
            {
                Material newMat;

                if ((flags & ObjectCloneFlags.CloneMaterials) != 0)
                    newMat = mat.Clone(flags);
                else
                    newMat = mat;

                mesh._materials.Add(newMat);
            }

            base.CloneWork(newObj, flags);
        }

        [Action("Export")]
        public void DoExport()
        {
            Export.Export();
        }

        public void NotifyBuffers(IBuffer<VertexData> vertices, IBuffer<uint>? indices)
        {
            VBuf = vertices;
            IBuf = indices;
        }

        protected void UpdateCompression()
        {
            if (_geometry == null)
                return;

            var oldVertComp = CompVertexType;
            var oldIndexComp = CompIndexType;

            bool compressIndices = true;

            if (CompressionMode == MeshCompressionMode.Always)
            {
                CompVertexType = typeof(CompVertexData);
            }
            else if (CompressionMode == MeshCompressionMode.Never || _geometry.Vertices.Length < 128)
            {
                CompVertexType = null; 
                CompIndexType = null;
                compressIndices = false;
            }
            else
            {
                if (Materials.Any(a => a.IsEnabled && (a.UseMorph || a.UseSkin)))
                    CompVertexType = null;
                else
                    CompVertexType = typeof(CompVertexData);
            }

            if (compressIndices)
            {
                if (_geometry.Indices.Length > 0 && _geometry.Vertices.Length <= byte.MaxValue + 1)
                    CompIndexType = typeof(byte);
                else if (_geometry.Indices.Length > 0 && _geometry.Vertices.Length <= ushort.MaxValue + 1)
                    CompIndexType = typeof(ushort);
                else
                    CompIndexType = null;
            }

            if (oldIndexComp != CompIndexType || oldVertComp != CompVertexType)
            {
                if (_geometry.IsGpuLoaded)
                    throw new InvalidOperationException();
                _geometry.Invalidate();
            }
        }

        unsafe void ICompressedVertexSource.CompressVertices(void* pSrc, void* pDst, int count)
        {
            Debug.Assert(_geometry != null);

            _geometry.EnsureId();

            Log.Debug(this, "Compress Mesh '{0}', geo '{1}'", (Name ?? GetType().Name), _geometry.Id);

#if DEBUG
            if (Materials.Any(a => a.UseMorph || a.UseSkin))
                throw new NotSupportedException();
#endif

            if (CompVertexType == typeof(CompVertexData))
            {

                var bounds = LocalBounds;
                var size = bounds.Max - bounds.Min;

                _geometry.VerticesRemap = Matrix4x4.CreateScale(size) * 
                                          Matrix4x4.CreateTranslation(bounds.Min);

                EngineNativeLib.CompressVertices(pSrc, pDst, count, _geometry.ActiveComponents, bounds);
                return;
            }

            throw new NotSupportedException();
        }

        unsafe void ICompressedVertexSource.CompressIndices(void* pSrc, void* pDst, int count)
        {
            if (CompIndexType == typeof(ushort))
            {
                EngineNativeLib.CompressIndices16(pSrc, pDst, count);
                return;
            }

            if (CompIndexType == typeof(byte))
            {
                EngineNativeLib.CompressIndices8(pSrc, pDst, count);
                return;
            }

            throw new NotSupportedException();
        }

        public Bounds3 LocalBounds
        {
            get
            {
                if (_localBoundsDirty && BoundUpdateMode == UpdateMode.Automatic)
                    UpdateBounds();
                return _localBounds;
            }
        }

        public Geometry3D? Geometry
        {
            get => _geometry;
            set
            {
                if (_geometry == value)
                    return;

                _geometry?.Detach(this);

                _geometry = value;

                _geometry?.Attach(this);

                UpdateCompression();

                InvalidateLocalBounds();

                NotifyChanged(ChangeType.Geometry);
            }
        }

        public Geometry3D? OriginalGeometry => _originalGeometry;

        public IList<Material> Materials => _materials;

        [Editable(false)]
        public IBuffer<VertexData>? VBuf { get; internal set; }

        [Editable(false)]
        public IBuffer<uint>? IBuf { get; internal set; }

        public int RenderPriority { get; set; }

        public UpdateMode BoundUpdateMode { get; set; }

        public MeshExportInfo<TriangleMesh> Export { get; set; }

        public int InstanceCount { get; set; }

        public Type? CompVertexType { get; set; }

        public Type? CompIndexType { get; set; }

        public MeshCompressionMode CompressionMode { get; set; }

        #region IVertexSource

        EngineObject IVertexSource.Host => _geometry!;

        VertexComponent IVertexSource.ActiveComponents => _geometry?.ActiveComponents ?? VertexComponent.None;

        DrawPrimitive IVertexSource.Primitive => _geometry?.Primitive ?? DrawPrimitive.Triangle;

        uint[] IVertexSource<VertexData, uint>.Indices => _geometry?.Indices ?? [];

        VertexData[] IVertexSource<VertexData, uint>.Vertices => _geometry?.Vertices ?? [];

        IReadOnlyList<Material> IVertexSource.Materials => _materials;

        Matrix4x4 ICompressedVertexSource.VerticesRemap => _geometry?.VerticesRemap ?? Matrix4x4.Identity;


        #endregion
    }
}
