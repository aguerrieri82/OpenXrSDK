using System.Numerics;

namespace XrEngine
{
    public class Cylinder3D : Geometry3D, IGeneratedContent
    {
        public Cylinder3D()
            : this(0.5f, 1f, 15)
        {
        }

        public Cylinder3D(float radius, float height, uint subs = 15, CylinderPart parts = CylinderPart.All)
        {
            Subs = subs;
            Radius = radius;
            Height = height;
            Parts = parts;
            Flags |= EngineObjectFlags.Readonly;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            if ((Parts & CylinderPart.Body) != 0)
                builder.AddCylinder(Center, Radius, Height, Subs);

            var smoothEnd = builder.Vertices.Count;

            if ((Parts & CylinderPart.BottomCap) != 0)
                builder.AddCircle(Center, Radius, Subs);

            if ((Parts & CylinderPart.TopCap) != 0)
                builder.AddCircle(Center + new Vector3(0, 0, Height), Radius, Subs, true);

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal;

            this.SmoothNormals(0, (uint)smoothEnd - 1);

            this.ComputeIndices();

            NotifyChanged(ObjectChangeType.Geometry);
        }

        public Vector3 Center { get; set; }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Height { get; set; }

        public CylinderPart Parts { get; set; }
    }
}
