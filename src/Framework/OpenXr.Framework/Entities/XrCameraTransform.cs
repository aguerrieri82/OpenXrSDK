using Silk.NET.OpenXR;
using System.Numerics;

namespace OpenXr.Framework
{
    public class XrCameraTransform
    {
        public Matrix4x4 Projection;

        public Matrix4x4 View;

        public static XrCameraTransform FromView(CompositionLayerProjectionView view, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            var result = new XrCameraTransform();

            result.Projection = XrMath.CreateProjectionFov(
                   MathF.Tan(view.Fov.AngleLeft),
                   MathF.Tan(view.Fov.AngleRight),
                   MathF.Tan(reverseUpDown ? view.Fov.AngleDown : view.Fov.AngleUp),
                   MathF.Tan(reverseUpDown ? view.Fov.AngleUp : view.Fov.AngleDown),
                   nearPlane,
                   farPlane);

            var pose = view.Pose.ToXrPose();

            var matrix = (Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                          Matrix4x4.CreateTranslation(pose.Position));

            Matrix4x4.Invert(matrix, out result.View);

            return result;
        }
    }
}
