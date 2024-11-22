namespace XrEngine
{

    public class LineMesh : Object3D, IVertexSource<PointData, uint>
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

        public PointData[] Vertices { get; set; }

        public LineMaterial Material { get; }

        public VertexComponent ActiveComponents { get; set; }

        public int RenderPriority { get; set; }


        #region IVertexSource

        EngineObject IVertexSource.Object => this!;

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Line;

        uint[] IVertexSource<PointData, uint>.Indices => [];

        PointData[] IVertexSource<PointData, uint>.Vertices => Vertices;

        IReadOnlyList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
