namespace XrEngine
{

    public class PointMesh : Object3D, IVertexSource<PointData, uint>
    {
        public PointMesh()
        {
            Material = new PointMaterial();
            Vertices = [];
            ActiveComponents = VertexComponent.Position | VertexComponent.Color4 | VertexComponent.Size;
        }

        public void NotifyLoaded()
        {

        }

        public PointData[] Vertices { get; set; }

        public PointMaterial Material { get; }

        public VertexComponent ActiveComponents { get; set; }

        public int RenderPriority { get; set; }


        #region IVertexSource

        EngineObject IVertexSource.Object => this!;

        DrawPrimitive IVertexSource.Primitive => DrawPrimitive.Point;

        uint[] IVertexSource<PointData, uint>.Indices => [];

        PointData[] IVertexSource<PointData, uint>.Vertices => Vertices;

        IReadOnlyList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
