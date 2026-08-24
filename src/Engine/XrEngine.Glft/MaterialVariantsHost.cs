using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Gltf
{
    public class MaterialVariant
    {
        private readonly List<Binding> _binds = [];

        public struct Binding
        {
            public Material Material;

            public TriangleMesh Mesh;
        }

        public void Bind(TriangleMesh mesh, Material material)
        {
            _binds.Add(new Binding
            {
                Mesh = mesh,
                Material = material
            });
        }

        public List<Binding> Bindings => _binds;

        public string? Name;

    }

    public class MaterialVariantsHost : BaseComponent<Object3D>
    {
        private int _activeVariant;
        private readonly List<MaterialVariant> _variants = [];

        protected void SelectVariant()
        {
            for (var i = 0; i < _variants.Count; i++)
            {
                bool isActive = i == _activeVariant;
                foreach (var item in _variants[i].Bindings)
                    item.Material.IsEnabled = isActive;
            }
        }

        public int ActiveVariant
        {
            get => _activeVariant;
            set
            {
                if (_activeVariant == value)
                    return;
                _activeVariant = value;
                SelectVariant();
            }
        }


        public List<MaterialVariant> Variants => _variants;
    }
}
