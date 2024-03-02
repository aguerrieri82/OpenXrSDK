using Silk.NET.OpenXR;
using System.Numerics;

namespace OpenXr.Framework
{
    public class XrCameraTransform
    {
        public Matrix4x4 Projection;

        public Matrix4x4 Transform;

        public static XrCameraTransform FromView(CompositionLayerProjectionView view, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            return FromView(view.Pose, view.Fov, nearPlane, farPlane, reverseUpDown);
        }

        public static XrCameraTransform FromView(View view, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            return FromView(view.Pose, view.Fov, nearPlane, farPlane, reverseUpDown);
        }

        public static XrCameraTransform FromView(Posef pose, Fovf fov, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            var xrPose = pose.ToXrPose();

            var result = new XrCameraTransform();

            result.Projection = XrMath.CreateProjectionFov(
                   MathF.Tan(fov.AngleLeft),
                   MathF.Tan(fov.AngleRight),
                   MathF.Tan(reverseUpDown ? fov.AngleDown : fov.AngleUp),
                   MathF.Tan(reverseUpDown ? fov.AngleUp : fov.AngleDown),
                   nearPlane,
                   farPlane);

            result.Transform = (Matrix4x4.CreateFromQuaternion(xrPose.Orientation) *
                          Matrix4x4.CreateTranslation(xrPose.Position));

            return result;
        }
    }
}
