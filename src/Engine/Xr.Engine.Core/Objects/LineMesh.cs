namespace Xr.Engine
{

    public class LineMesh : Object3D, IVertexSource<LineData, uint>
    {

        public LineMesh()
        {
            Material = new LineMaterial();
            Vertices = [];
            ActiveComponents = VertexComponent.Position | VertexComponent.Color3;
        }

        public LineData[] Vertices { get; set; }

        public LineMaterial Material { get; }

        public VertexComponent ActiveComponents { get; set; }


        #region IVertexSource

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Line;

        uint[] IVertexSource<LineData, uint>.Indices => [];

        LineData[] IVertexSource<LineData, uint>.Vertices => Vertices;

        IList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
