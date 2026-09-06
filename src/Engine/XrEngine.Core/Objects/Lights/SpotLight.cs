using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class SpotLight : Light
    {
        public SpotLight()
        {
            Range = 6.0f;
            Intensity = 1.0f;
            InnerConeAngle = MathF.PI / 10.0f; // 18°
            OuterConeAngle = MathF.PI / 6.0f;  // 30°
            Color = "#ffffff";
        }

        public override void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            var origin = WorldPosition;
            var direction = Vector3.Normalize(Direction);
            var baseCenter = origin + direction * Range;

            var outerRadius = MathF.Tan(OuterConeAngle) * Range;
            var innerRadius = MathF.Tan(InnerConeAngle) * Range;

            var cameraVector = ctx.Camera!.WorldPosition - origin;

            // Direction around the cone facing the camera.
            var radial0 =
                cameraVector -
                direction * Vector3.Dot(cameraVector, direction);

            if (radial0.LengthSquared() < 0.000001f)
            {
                radial0 = Vector3.Cross(
                    direction,
                    MathF.Abs(direction.Y) < 0.999f
                        ? Vector3.UnitY
                        : Vector3.UnitX);
            }

            radial0 = Vector3.Normalize(radial0);

            // Second axial plane, rotated 90 degrees around the cone axis.
            var radial1 =
                Vector3.Normalize(Vector3.Cross(direction, radial0));

            // DrawCircle local plane is XY, with normal +Z.
            var zDot = Vector3.Dot(Vector3.UnitZ, direction);

            Quaternion circleOrientation;

            if (zDot < -0.999999f)
            {
                circleOrientation =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
            }
            else
            {
                var axis = Vector3.Cross(Vector3.UnitZ, direction);

                circleOrientation = Quaternion.Normalize(
                    new Quaternion(
                        axis.X,
                        axis.Y,
                        axis.Z,
                        1.0f + zDot));
            }

            var basePose = new Pose3
            {
                Position = baseCenter,
                Orientation = circleOrientation
            };

            canvas.DrawCircle(basePose, outerRadius);
            canvas.DrawCircle(basePose, innerRadius);

            // Camera-facing axial cone section.
            canvas.DrawLine(origin, baseCenter + radial0 * outerRadius);
            canvas.DrawLine(origin, baseCenter - radial0 * outerRadius);

            // Axial cone section rotated 90 degrees.
            canvas.DrawLine(origin, baseCenter + radial1 * outerRadius);
            canvas.DrawLine(origin, baseCenter - radial1 * outerRadius);

            // Inner cone sections.
            canvas.DrawLine(origin, baseCenter + radial0 * innerRadius);
            canvas.DrawLine(origin, baseCenter - radial0 * innerRadius);

            canvas.DrawLine(origin, baseCenter + radial1 * innerRadius);
            canvas.DrawLine(origin, baseCenter - radial1 * innerRadius);
        }

        [ValueType(ValueType.Direction)]
        public Vector3 Direction { get; set; }

        [Range(0, 100, 0.05f)]
        public float Range { get; set; }

        [ValueType(ValueType.Radiant)]
        public float InnerConeAngle { get; set; }

        [ValueType(ValueType.Radiant)]
        public float OuterConeAngle { get; set; }
    }
}
