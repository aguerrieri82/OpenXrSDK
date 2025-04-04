﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using XrMath;

namespace XrEngine
{
    public class TriangleMesh : Object3D, IVertexSource<VertexData, uint>, ILocalBounds
    {
        protected readonly ObservableCollection<Material> _materials;
        protected Geometry3D? _geometry;

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
            _materials.Update(ctx);

            Geometry?.Update(ctx);

            base.Update(ctx);
        }

        private void OnMaterialsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems!.Cast<Material>())
                        item.Detach(this, false);

                    NotifyChanged(new ObjectChange(ObjectChangeType.MateriaRemove, e.OldItems));
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.Cast<Material>())
                    item.Attach(this);

                NotifyChanged(new ObjectChange(ObjectChangeType.MateriaAdd, e.NewItems));
            }
        }

        public void NotifyLoaded()
        {
            _geometry?.NotifyLoaded();
        }

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

                NotifyChanged(ObjectChangeType.Geometry);
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

        public int RenderPriority { get; set; }

        public IList<Material> Materials => _materials;

        public Bounds3 LocalBounds => _geometry?.Bounds ?? Bounds3.Zero;

        public UpdateMode BoundUpdateMode { get; set; }

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
