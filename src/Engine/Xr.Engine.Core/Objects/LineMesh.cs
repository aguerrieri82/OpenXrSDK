using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;

namespace OpenXr.Engine
{
    public struct LineData
    {
        [ShaderRef(0, "vPos")]
        public Vector3 Pos;
        
        [ShaderRef(1, "vColor")]
        public Vector3 Color;

        [ShaderRef(2, "vSize")]
        public float Size;
    }

    public class LineMesh : Object3D, IVertexSource<LineData, uint> 
    {

        public LineMesh()
        {
            Material = new LineMaterial();
            Vertices = [];
        }

        public LineData[] Vertices { get; set; }  

        public LineMaterial Material { get; }

        #region IVertexSource

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Line;

        uint[] IVertexSource<LineData, uint>.Indices => [];

        LineData[] IVertexSource<LineData, uint>.Vertices => Vertices;

        IList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
