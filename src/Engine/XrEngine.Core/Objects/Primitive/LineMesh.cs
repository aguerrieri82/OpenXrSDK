
namespace XrEngine
{

    public class LineMesh : Object3D, IVertexSource
    {
        public LineMesh()
        {
            Material = new LineMaterial();
            Material.Attach(this);
            Vertices = [];
            Flags |= EngineObjectFlags.NoLogs;
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

        Array IVertexSource.Indices => Array.Empty<uint>();

        Array IVertexSource.Vertices => Vertices;

        IReadOnlyList<Material> IVertexSource.Materials => [Material];

        #endregion
    }
}
