using System.Numerics;

namespace XrEngine
{
    public class CurvedScreen3D : Geometry3D, IGeneratedContent
    {
        public CurvedScreen3D()
            : this(2, 1, MathF.PI / 4, 64)
        {
        }

        public CurvedScreen3D(float width, float height, float curvature, uint subs = 64)
        {
            Width = width;
            Height = height;
            Curvature = curvature;
            Subs = subs;

            Flags |= EngineObjectFlags.Readonly;

            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            var radius = Width / Curvature;
            var step = Curvature / Subs;
            var y0 = -Height * 0.5f;
            var y1 = Height * 0.5f;

            for (var i = 0; i < Subs; i++)
            {
                var a1 = -Curvature * 0.5f + step * i;
                var a2 = -Curvature * 0.5f + step * (i + 1);

                var u1 = (float)i / Subs;
                var u2 = (float)(i + 1) / Subs;

                var p1 = new Vector3(MathF.Sin(a1) * radius, y0, radius - MathF.Cos(a1) * radius);
                var p2 = new Vector3(MathF.Sin(a2) * radius, y0, radius - MathF.Cos(a2) * radius);
                var p3 = new Vector3(p1.X, y1, p1.Z);
                var p4 = new Vector3(p2.X, y1, p2.Z);

                builder.AddFace(
                    p1, p2, p4, p3,
                    new Vector2(u1, 1), new Vector2(u2, 1), new Vector2(u2, 0), new Vector2(u1, 0),
                    false);
            }

            Vertices = builder.Vertices.ToArray();
            Indices = [];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;

            for (var i = 0; i < Vertices.Length; i++)
            {
                var pos = Vertices[i].Pos;
                Vertices[i].Normal = -Vector3.Normalize(new Vector3(pos.X, 0, pos.Z - radius));
            }

            this.ComputeIndices();
        }

        public float Width { get; set; }

        public float Height { get; set; }

        [ValueType(ValueType.Radiant)]
        public float Curvature { get; set; }

        public uint Subs { get; set; }
    }
}