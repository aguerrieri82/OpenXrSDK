using System.Numerics;

namespace XrEngine
{
    public class Donut3D : Geometry3D, IGeneratedContent
    {
        public Donut3D()
            : this(1, 0.8f, 0.01f, 32)
        {

        }

        public Donut3D(float radius, float innerRadius, float height, uint subs)
        {
            Flags |= EngineObjectFlags.Readonly;
            Radius = radius;
            InnerRadius = innerRadius;
            Height = height;
            Subs = subs;
            Build();
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            var c1 = new Vector3(0, 0, Height / 2);
            var c2 = new Vector3(0, 0, -Height / 2);

            for (var i = 0; i < Subs; i++)
            {
                var a1 = MathF.PI * 2 * i / Subs;
                var a2 = MathF.PI * 2 * (i + 1) / Subs;

                var v1 = c1 + new Vector3(MathF.Cos(a1) * Radius, MathF.Sin(a1) * Radius, 0);
                var v2 = c1 + new Vector3(MathF.Cos(a2) * Radius, MathF.Sin(a2) * Radius, 0);

                var v3 = c1 + new Vector3(MathF.Cos(a1) * InnerRadius, MathF.Sin(a1) * InnerRadius, 0);
                var v4 = c1 + new Vector3(MathF.Cos(a2) * InnerRadius, MathF.Sin(a2) * InnerRadius, 0);

                builder.AddFace(v1, v2, v4, v3);

                if (Height > 0)
                {
                    var vv1 = c2 + new Vector3(MathF.Cos(a1) * Radius, MathF.Sin(a1) * Radius, 0);
                    var vv2 = c2 + new Vector3(MathF.Cos(a2) * Radius, MathF.Sin(a2) * Radius, 0);

                    var vv3 = c2 + new Vector3(MathF.Cos(a1) * InnerRadius, MathF.Sin(a1) * InnerRadius, 0);
                    var vv4 = c2 + new Vector3(MathF.Cos(a2) * InnerRadius, MathF.Sin(a2) * InnerRadius, 0);

                    builder.AddFace(vv1, vv2, vv4, vv3, true);
                    builder.AddFace(v1, v2, vv2, vv1, true);
                    builder.AddFace(v3, v4, vv4, vv3);
                }
            }

            Vertices = builder.Vertices.ToArray();


            ActiveComponents |= VertexComponent.Position | VertexComponent.Normal;

            this.ComputeIndices();

            Version++;
        }

        public uint Subs { get; set; }

        public float Radius { get; set; }

        public float InnerRadius { get; set; }

        public float Height { get; set; }

    }
}
