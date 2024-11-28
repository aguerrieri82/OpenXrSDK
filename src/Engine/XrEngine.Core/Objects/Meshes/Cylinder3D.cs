using System.Numerics;

namespace XrEngine
{
    public class Cylinder3D : Geometry3D, IGeneratedContent
    {
        public Cylinder3D()
            : this(0.5f, 1f, 15)
        {
        }

        public Cylinder3D(float radius, float height, uint subs = 15)
        {
            Subs = subs;
            Radius = radius;
            Height = height;
            Flags |= EngineObjectFlags.Readonly;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            builder.AddCylinder(Center, Radius, Height, Subs);
            var smoothEnd = builder.Vertices.Count;
            builder.AddCircle(Center, Radius, Subs);
            builder.AddCircle(Center + new Vector3(0, 0, Height), Radius, Subs, true);

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal;

            this.SmoothNormals(0, (uint)smoothEnd);

            this.ComputeIndices();

            NotifyChanged(ObjectChangeType.Geometry);
        }

        public Vector3 Center { get; set; }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Height { get; set; }
    }
}
