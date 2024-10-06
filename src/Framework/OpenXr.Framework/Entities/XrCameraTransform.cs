using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace OpenXr.Framework
{
    public struct XrCameraTransform
    {
        public Matrix4x4 Projection;

        public Matrix4x4 Transform;

        public static XrCameraTransform FromView(CompositionLayerProjectionView view, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            return FromView(view.Pose.ToPose3(), view.Fov, nearPlane, farPlane, reverseUpDown);
        }

        public static XrCameraTransform FromView(View view, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            return FromView(view.Pose.ToPose3(), view.Fov, nearPlane, farPlane, reverseUpDown);
        }

        public static XrCameraTransform FromView(Pose3 pose, Fovf fov, float nearPlane, float farPlane, bool reverseUpDown = false)
        {
            var result = new XrCameraTransform();

            result.Projection = CreateProjectionFov(
                   MathF.Tan(fov.AngleLeft),
                   MathF.Tan(fov.AngleRight),
                   MathF.Tan(reverseUpDown ? fov.AngleDown : fov.AngleUp),
                   MathF.Tan(reverseUpDown ? fov.AngleUp : fov.AngleDown),
                   nearPlane,
                   farPlane);

            result.Transform = (Matrix4x4.CreateFromQuaternion(pose.Orientation) *
                                Matrix4x4.CreateTranslation(pose.Position));

            return result;
        }


        unsafe static Matrix4x4 CreateProjectionFov(float tanAngleLeft,
                                                     float tanAngleRight,
                                                     float tanAngleUp,
                                                     float tanAngleDown,
                                                     float nearZ,
                                                     float farZ)
        {
            var result = stackalloc float[16];

            float tanAngleWidth = tanAngleRight - tanAngleLeft;
            float tanAngleHeight = tanAngleUp - tanAngleDown;
            float offsetZ = nearZ;

            if (farZ <= nearZ)
            {
                // place the far plane at infinity
                result[0] = 2.0f / tanAngleWidth;
                result[4] = 0.0f;
                result[8] = (tanAngleRight + tanAngleLeft) / tanAngleWidth;
                result[12] = 0.0f;

                result[1] = 0.0f;
                result[5] = 2.0f / tanAngleHeight;
                result[9] = (tanAngleUp + tanAngleDown) / tanAngleHeight;
                result[13] = 0.0f;

                result[2] = 0.0f;
                result[6] = 0.0f;
                result[10] = -1.0f;
                result[14] = -(nearZ + offsetZ);

                result[3] = 0.0f;
                result[7] = 0.0f;
                result[11] = -1.0f;
                result[15] = 0.0f;
            }
            else
            {
                // normal projection
                result[0] = 2.0f / tanAngleWidth;
                result[4] = 0.0f;
                result[8] = (tanAngleRight + tanAngleLeft) / tanAngleWidth;
                result[12] = 0.0f;

                result[1] = 0.0f;
                result[5] = 2.0f / tanAngleHeight;
                result[9] = (tanAngleUp + tanAngleDown) / tanAngleHeight;
                result[13] = 0.0f;

                result[2] = 0.0f;
                result[6] = 0.0f;
                result[10] = -(farZ + offsetZ) / (farZ - nearZ);
                result[14] = -(farZ * (nearZ + offsetZ)) / (farZ - nearZ);

                result[3] = 0.0f;
                result[7] = 0.0f;
                result[11] = -1.0f;
                result[15] = 0.0f;
            }

            return *(Matrix4x4*)result;
        }
    }
}
