using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class DirectionalLight : Light
    {
        public DirectionalLight()
        {
        }

        public DirectionalLight(Vector3 direction)
        {
            Direction = direction;
        }

        public override void DrawGizmos(Canvas3D canvas)
        {
            const float ArrowLength = 70.0f;
            const float ArrowSpacing = 14.0f;
            const float ArrowTipLength = 12.0f;

            var camera = _scene!.ActiveCamera!;
            Vector2 viewSize = camera.ViewSize.ToVector2();
            Matrix4x4 viewProjection = camera.ViewProjection;

            Vector3 direction = Vector3.Normalize(Direction);
            Vector3 toCamera = camera.WorldPosition - WorldPosition;

            // Plane containing the arrows, oriented for maximum camera visibility.
            Vector3 planeNormal =
                toCamera - direction * Vector3.Dot(toCamera, direction);

            if (planeNormal.LengthSquared() < 0.000001f)
                return;

            planeNormal = Vector3.Normalize(planeNormal);

            Vector3 side = Vector3.Normalize(
                Vector3.Cross(planeNormal, direction));

            // Uniform world-units-per-pixel scale on the camera plane
            // passing through the gizmo position.
            Vector4 clip = Vector4.Transform(
                new Vector4(WorldPosition, 1.0f),
                viewProjection);

            Vector3 ndc = new(
                clip.X / clip.W,
                clip.Y / clip.W,
                clip.Z / clip.W);

            Matrix4x4.Invert(
                viewProjection,
                out Matrix4x4 inverseViewProjection);

            Vector4 pixelWorld4 = Vector4.Transform(
                new Vector4(
                    ndc.X + 2.0f / viewSize.X,
                    ndc.Y,
                    ndc.Z,
                    1.0f),
                inverseViewProjection);

            Vector3 pixelWorld = new(
                pixelWorld4.X / pixelWorld4.W,
                pixelWorld4.Y / pixelWorld4.W,
                pixelWorld4.Z / pixelWorld4.W);

            float scale = Vector3.Distance(
                WorldPosition,
                pixelWorld);

            canvas.Save();

            canvas.State.Color = "#ffff00";

            canvas.State.Transform = new Matrix4x4(
                direction.X * scale,
                direction.Y * scale,
                direction.Z * scale,
                0.0f,

                side.X * scale,
                side.Y * scale,
                side.Z * scale,
                0.0f,

                planeNormal.X * scale,
                planeNormal.Y * scale,
                planeNormal.Z * scale,
                0.0f,

                WorldPosition.X,
                WorldPosition.Y,
                WorldPosition.Z,
                1.0f);

            for (int i = -1; i <= 1; i++)
            {
                float y = i * ArrowSpacing;

                canvas.DrawLine(
                    new Vector3(0.0f, y, 0.0f),
                    new Vector3(ArrowLength, y, 0.0f));

                canvas.DrawLine(
                    new Vector3(ArrowLength, y, 0.0f),
                    new Vector3(
                        ArrowLength - ArrowTipLength,
                        y + ArrowSpacing * 0.4f,
                        0.0f));

                canvas.DrawLine(
                    new Vector3(ArrowLength, y, 0.0f),
                    new Vector3(
                        ArrowLength - ArrowTipLength,
                        y - ArrowSpacing * 0.4f,
                        0.0f));
            }

            canvas.Restore();
        }

        [ValueType(ValueType.Direction)]
        public Vector3 Direction { get; set; }

    }
}
