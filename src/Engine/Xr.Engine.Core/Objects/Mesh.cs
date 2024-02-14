using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OpenXr.Engine
{
    public class Mesh : Object3D
    {
        readonly ObservableCollection<Material> _materials;

        public Mesh()
        {
            _materials = [];
            _materials.CollectionChanged += OnMaterialsChanged;
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
                    item.Attach(this);
            }

            NotifyChanged(ObjectChangeType.Render);
        }

        public Mesh(Geometry geometry, Material? material = null)
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

        public Geometry? Geometry { get; set; }
    }
}
