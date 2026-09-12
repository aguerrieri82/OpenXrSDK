using System.Numerics;

namespace XrEngine
{
    public class Cylinder3D : Geometry3D, IGeneratedContent
    {
        public Cylinder3D()
            : this(0.5f, 1f, 15)
        {
        }

        public Cylinder3D(float radius, float height, uint subs = 15, CylinderPart parts = CylinderPart.All, UVMode uvMode = UVMode.Normalized, float angle = MathF.Tau)
        {
            Subs = subs;
            Radius = radius;
            Height = height;
            Parts = parts;
            Flags |= EngineObjectFlags.Readonly;
            UVMode = uvMode;
            Angle = angle;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            if ((Parts & CylinderPart.Body) != 0)
                builder.AddCylinder(Center, Radius, Height, Subs, UVMode, Angle);

            var smoothEnd = builder.Vertices.Count;

            if ((Parts & CylinderPart.BottomCap) != 0)
                builder.AddCircle(Center, Radius, Subs, false, UVMode, Angle);

            if ((Parts & CylinderPart.TopCap) != 0)
                builder.AddCircle(Center + new Vector3(0, 0, Height), Radius, Subs, true, UVMode, Angle);

            if (Angle < MathF.Tau)
            {
                var uvRadius = UVMode == UVMode.Normalized ? 1 : Radius;
                var uvHeight = UVMode == UVMode.Normalized ? 1 : Height;

                var bottomCenter = Center;
                var topCenter = Center + new Vector3(0, 0, Height);

                if ((Parts & CylinderPart.StartCut) != 0)
                {
                    var dir = new Vector3(1, 0, 0);
                    var bottomEdge = bottomCenter + dir * Radius;
                    var topEdge = topCenter + dir * Radius;

                    builder.AddFace(
                        bottomCenter, bottomEdge, topEdge, topCenter,
                        new Vector2(0, 0),
                        new Vector2(uvRadius, 0),
                        new Vector2(uvRadius, uvHeight),
                        new Vector2(0, uvHeight));
                }

                if ((Parts & CylinderPart.EndCut) != 0)
                {
                    var dir = new Vector3(MathF.Cos(Angle), MathF.Sin(Angle), 0);
                    var bottomEdge = bottomCenter + dir * Radius;
                    var topEdge = topCenter + dir * Radius;

                    builder.AddFace(
                        bottomEdge, bottomCenter, topCenter, topEdge,
                        new Vector2(uvRadius, 0),
                        new Vector2(0, 0),
                        new Vector2(0, uvHeight),
                        new Vector2(uvRadius, uvHeight));
                }
            }

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;

            if (smoothEnd > 0)
                this.SmoothNormals(0, (uint)smoothEnd - 1);

            this.ComputeIndices();

            NotifyChanged(ChangeType.Geometry);
        }

        public Vector3 Center { get; set; }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float Height { get; set; }

        [ValueType(ValueType.Radiant)]
        public float Angle { get; set; }

        public CylinderPart Parts { get; set; }

        public UVMode UVMode { get; set; }
    }
}