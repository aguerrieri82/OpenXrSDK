using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public class VirtualMesh : Object3D, IVirtualVertexSource
    {
        readonly Material[] _material = new Material[1];

        public VirtualMesh(Material material, uint verticesCount)
        {
            _material[0] = material;
            VerticesCount = verticesCount;
        }


        #region IVirtualVertexSource

        EngineObject IVertexSource.Host => this;


        void IGpuObject.NotifyLoaded()
        {
        }

        IReadOnlyList<Material> IVertexSource.Materials => _material[0] == null ? [] : _material;

        #endregion

        public Material Material => _material[0];

        public VertexComponent ActiveComponents { get; set; }

        public uint VerticesCount { get; set; }

        public DrawPrimitive Primitive { get; set; }

        public int RenderPriority { get; set; }

    }
}
