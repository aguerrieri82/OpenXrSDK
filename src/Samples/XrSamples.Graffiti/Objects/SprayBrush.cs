using System.Numerics;

using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class SprayBrush : TriangleMesh
    {
        public SprayBrush()
        {
            RadialSubs = 30;
            InnerSubs = 10;
            GridSubs = 20;

            CompressionMode = MeshCompressionMode.Never;
            Materials.Add(new WireframeMaterial() { Color = new Color(0, 1, 0), DoubleSided = true });
            IsVisible = false;

            SetMask(Vector2.One, 0);
        }

        public void SetMask(Vector2 size, float angle)
        {
            if (Geometry != null && size == MaskSize && angle == MaskAngle)
                return;

            MaskSize = size;
            MaskAngle = angle;

            var builder = new MeshBuilder();

            var radius = 0.5f;

            Vector2 ToUv(Vector3 pos) => new Vector2(pos.X + 0.5f, pos.Y + 0.5f);

            if (size.X >= 1 && size.Y >= 1)
            {
                var iStep = 1f / (InnerSubs) * radius;

                var jStep = 2 * MathF.PI / RadialSubs;

                for (var i = 0; i < InnerSubs; i++)
                {
                    var r1 = radius - (iStep * i);
                    var r2 = radius - (iStep * (i + 1));

                    for (var j = 0; j < RadialSubs; j++)
                    {
                        var a1 = jStep * j;
                        var a2 = jStep * (j + 1);

                        var p1 = new Vector3(r1 * MathF.Cos(a1), r1 * MathF.Sin(a1), 0);
                        var p2 = new Vector3(r1 * MathF.Cos(a2), r1 * MathF.Sin(a2), 0);

                        if (i == InnerSubs - 1)
                            builder.AddTriangle(p1, p2, Vector3.Zero, ToUv(p1), ToUv(p2), ToUv(Vector3.Zero));
                        else
                        {
                            var p3 = new Vector3(r2 * MathF.Cos(a1), r2 * MathF.Sin(a1), 0);
                            var p4 = new Vector3(r2 * MathF.Cos(a2), r2 * MathF.Sin(a2), 0);
                            builder.AddFace(p1, p2, p4, p3, ToUv(p1), ToUv(p2), ToUv(p4), ToUv(p3));
                        }
                    }
                }
            }
            else
            {
                var half = Vector2.Min(size, Vector2.One) * radius;
                var rotation = Matrix3x2.CreateRotation(angle);

                Vector3 GridPoint(int i, int j)
                {
                    var t = MathF.PI / 2 * (2f * i / GridSubs - 1);
                    var x = half.X * MathF.Sin(t);
                    var extent = half.Y * MathF.Cos(t);
                    var p = Vector2.Transform(new Vector2(x, extent * (2f * j / GridSubs - 1)), rotation);
                    return new Vector3(p.X, p.Y, 0);
                }

                for (var i = 0; i < GridSubs; i++)
                {
                    for (var j = 0; j < GridSubs; j++)
                    {
                        var p1 = GridPoint(i, j);
                        var p2 = GridPoint(i + 1, j);
                        var p3 = GridPoint(i + 1, j + 1);
                        var p4 = GridPoint(i, j + 1);
                        builder.AddFace(p1, p2, p3, p4, ToUv(p1), ToUv(p2), ToUv(p3), ToUv(p4));
                    }
                }
            }

            Geometry = builder.ToGeometry();
        }

        public Vector2 MaskSize { get; private set; }

        public float MaskAngle { get; private set; }

        public int RadialSubs { get; set; }

        public int InnerSubs { get; set; }

        public int GridSubs { get; set; }
    }
}
