using System.Numerics;

namespace XrEngine
{
    public class Arrow3D : Geometry3D, IGeneratedContent
    {
        public Arrow3D()
        {
            Subs = 15;
            BaseDiameter = 0.03f;
            ArrowDiameter = 0.075f;
            ArrowLength = 0.1f;
            BaseLength = 0.2f;
            Flags |= EngineObjectFlags.Readonly;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            builder.AddCircle(Vector3.Zero, BaseDiameter / 2, Subs);
            builder.AddCircle(new Vector3(0, 0, BaseLength), ArrowDiameter / 2, Subs);
            var smoothStart = builder.Vertices.Count;
            builder.AddCylinder(Vector3.Zero, BaseDiameter / 2, BaseLength, Subs);
            builder.AddCone(new Vector3(0, 0, BaseLength), ArrowDiameter / 2, ArrowLength, Subs);

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal;

            this.SmoothNormals((uint)smoothStart, (uint)Vertices.Length - 1);

            this.ComputeIndices();

            NotifyChanged(ObjectChangeType.Geometry);
        }

        public uint Subs { get; set; }

        public float BaseDiameter { get; set; }

        public float ArrowDiameter { get; set; }

        public float ArrowLength { get; set; }

        public float BaseLength { get; set; }


    }
}
