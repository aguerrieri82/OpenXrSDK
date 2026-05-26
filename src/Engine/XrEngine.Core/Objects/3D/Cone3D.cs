using System.Numerics;

namespace XrEngine
{
    public class Cone3D : Geometry3D, IGeneratedContent
    {
        public Cone3D()
            : this(0.5f, 1f, 15)
        {
        }

        public Cone3D(float radius, float height, uint subs = 15, UVMode uvMode = UVMode.Normalized)
        {
            Subs = subs;
            Radius = radius;
            Height = height;
            Flags |= EngineObjectFlags.Readonly;
            UVMode = uvMode;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            builder.AddCone(Center, Radius, Height, Subs);

            var smoothEnd = builder.Vertices.Count;

            builder.AddCircle(Center, Radius, Subs, false, UVMode);

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;

            this.SmoothNormals(0, (uint)smoothEnd - 1);

            this.ComputeIndices();

            NotifyChanged(ObjectChangeType.Geometry);
        }

        public Vector3 Center { get; set; }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Height { get; set; }

        public CylinderPart Parts { get; set; }

        public UVMode UVMode { get; set; }
    }
}
