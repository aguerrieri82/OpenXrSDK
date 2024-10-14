namespace XrEngine
{

    public class LineMesh : Object3D, IVertexSource<LineData, uint>
    {
        public LineMesh()
        {
            Material = new LineMaterial();
            Vertices = [];
            ActiveComponents = VertexComponent.Position | VertexComponent.Color4;
        }

        public void NotifyLoaded()
        {

        }

        public LineData[] Vertices { get; set; }

        public LineMaterial Material { get; }

        public VertexComponent ActiveComponents { get; set; }


        #region IVertexSource

        EngineObject IVertexSource.Object => this!;

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Line;

        uint[] IVertexSource<LineData, uint>.Indices => [];

        LineData[] IVertexSource<LineData, uint>.Vertices => Vertices;

        IReadOnlyList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
