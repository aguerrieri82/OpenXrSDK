using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class AreaLight : DirectionalLight
    {
        public AreaLight()
        {
            PlaneUp = Vector3.UnitY;
            Range = 5f;
        }

        public override void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            var normal = Vector3.Normalize(PlaneNormal);
            var up = Vector3.Normalize(
                PlaneUp - normal * Vector3.Dot(PlaneUp, normal));
            var direction = Vector3.Normalize(Direction);

            if (normal.LengthSquared() < 0.000001f ||
                up.LengthSquared() < 0.000001f ||
                direction.LengthSquared() < 0.000001f)
            {
                return;
            }

            var right = Vector3.Normalize(
                Vector3.Cross(up, normal));

            var halfWidth = PlaneSize.X * 0.5f;
            var halfHeight = PlaneSize.Y * 0.5f;

            var p0 = WorldPosition - right * halfWidth - up * halfHeight;
            var p1 = WorldPosition + right * halfWidth - up * halfHeight;
            var p2 = WorldPosition + right * halfWidth + up * halfHeight;
            var p3 = WorldPosition - right * halfWidth + up * halfHeight;

            var rangeOffset = direction * Range;
            var p4 = p0 + rangeOffset;
            var p5 = p1 + rangeOffset;
            var p6 = p2 + rangeOffset;
            var p7 = p3 + rangeOffset;

            canvas.Save();
            canvas.State.Color = "#ffff00";

            canvas.DrawLine(p0, p1);
            canvas.DrawLine(p1, p2);
            canvas.DrawLine(p2, p3);
            canvas.DrawLine(p3, p0);

            canvas.DrawLine(p0, p2);
            canvas.DrawLine(p1, p3);

            canvas.DrawLine(p0, p4);
            canvas.DrawLine(p1, p5);
            canvas.DrawLine(p2, p6);
            canvas.DrawLine(p3, p7);

            canvas.State.Color = "#ffff80";

            canvas.DrawLine(p4, p5);
            canvas.DrawLine(p5, p6);
            canvas.DrawLine(p6, p7);
            canvas.DrawLine(p7, p4);

            canvas.Restore();
        }

        [Range(0, 100, 0.05f)]
        public float Range { get; set; }

        public Vector2 PlaneSize { get; set; }

        [ValueType(ValueType.Direction)]
        public Vector3 PlaneUp { get; set; }

        [ValueType(ValueType.Direction)]
        public Vector3 PlaneNormal { get; set; }
    }
}
