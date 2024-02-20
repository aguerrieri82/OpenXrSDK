using System.Numerics;

namespace OpenXr.Engine
{
    public struct LineData
    {
        [ShaderRef(0, "a_position", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(1, "a_color_0", VertexComponent.Color3)]
        public Vector3 Color;

        [ShaderRef(2, "a_size", VertexComponent.Generic)]
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
