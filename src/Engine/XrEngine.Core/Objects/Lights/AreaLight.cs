using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
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
            const float ArrowLength = 70.0f;
            const float ArrowTipLength = 12.0f;
            const float ArrowTipWidth = 6.0f;

            Vector3 normal = Vector3.Normalize(PlaneNormal);
            Vector3 up = Vector3.Normalize(
                PlaneUp - normal * Vector3.Dot(PlaneUp, normal));

            if (normal.LengthSquared() < 0.000001f ||
                up.LengthSquared() < 0.000001f)
            {
                return;
            }

            Vector3 right = Vector3.Normalize(
                Vector3.Cross(up, normal));

            float halfWidth = PlaneSize.X * 0.5f;
            float halfHeight = PlaneSize.Y * 0.5f;

            Vector3 p0 = WorldPosition - right * halfWidth - up * halfHeight;
            Vector3 p1 = WorldPosition + right * halfWidth - up * halfHeight;
            Vector3 p2 = WorldPosition + right * halfWidth + up * halfHeight;
            Vector3 p3 = WorldPosition - right * halfWidth + up * halfHeight;

            canvas.Save();
            canvas.State.Color = "#ffff00";

            canvas.DrawLine(p0, p1);
            canvas.DrawLine(p1, p2);
            canvas.DrawLine(p2, p3);
            canvas.DrawLine(p3, p0);

            canvas.DrawLine(p0, p2);
            canvas.DrawLine(p1, p3);

            canvas.Restore();

            Vector3 direction = Vector3.Normalize(Direction);

            if (direction.LengthSquared() < 0.000001f)
                return;

            var camera = ctx.Camera!;
            Vector2 viewSize = camera.ViewSize.ToVector2();
            Matrix4x4 viewProjection = camera.ViewProjection;

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

            Vector3 toCamera = camera.WorldPosition - WorldPosition;

            Vector3 arrowPlaneNormal =
                toCamera - direction * Vector3.Dot(toCamera, direction);

            if (arrowPlaneNormal.LengthSquared() < 0.000001f)
                arrowPlaneNormal = normal;

            arrowPlaneNormal = Vector3.Normalize(arrowPlaneNormal);

            Vector3 arrowSide = Vector3.Normalize(
                Vector3.Cross(arrowPlaneNormal, direction));

            canvas.Save();

            canvas.State.Color = "#ffff00";

            canvas.State.Transform = new Matrix4x4(
                direction.X * scale,
                direction.Y * scale,
                direction.Z * scale,
                0.0f,

                arrowSide.X * scale,
                arrowSide.Y * scale,
                arrowSide.Z * scale,
                0.0f,

                arrowPlaneNormal.X * scale,
                arrowPlaneNormal.Y * scale,
                arrowPlaneNormal.Z * scale,
                0.0f,

                WorldPosition.X,
                WorldPosition.Y,
                WorldPosition.Z,
                1.0f);

            canvas.DrawLine(
                Vector3.Zero,
                new Vector3(ArrowLength, 0.0f, 0.0f));

            canvas.DrawLine(
                new Vector3(ArrowLength, 0.0f, 0.0f),
                new Vector3(
                    ArrowLength - ArrowTipLength,
                    ArrowTipWidth,
                    0.0f));

            canvas.DrawLine(
                new Vector3(ArrowLength, 0.0f, 0.0f),
                new Vector3(
                    ArrowLength - ArrowTipLength,
                    -ArrowTipWidth,
                    0.0f));

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
