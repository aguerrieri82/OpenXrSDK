using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public static class XrMath
    {
        public static unsafe XrPose ToXrPose(this Posef pose)
        {
            return *(XrPose*)&pose;
        }

        public static XrPose Inverse(this XrPose pose)
        {
            return new XrPose
            {
                Orientation = Quaternion.Inverse(pose.Orientation),
                Position = Vector3.Transform(pose.Position * -1, pose.Orientation)
            };
        }

        public static Vector3 PoseTransform(XrPose a, Vector3 b)
        {
            var result = Vector3.Transform(b, a.Orientation);
            return result + a.Position;
        }

        public static XrPose PoseMultiply(XrPose a, XrPose b)
        {
            return new XrPose
            {
                Orientation = b.Orientation * a.Orientation,
                Position = PoseTransform(a, b.Position)
            };
        }

        public unsafe static Matrix4x4 InvertRigidBody(this Matrix4x4 src)
        {
            var result = stackalloc float[16];
            var srcArray = new Span<float>((float*)&src, 16);

            result[0] = srcArray[0];
            result[1] = srcArray[4];
            result[2] = srcArray[8];
            result[3] = 0.0f;
            result[4] = srcArray[1];
            result[5] = srcArray[5];
            result[6] = srcArray[9];
            result[7] = 0.0f;
            result[8] = srcArray[2];
            result[9] = srcArray[6];
            result[10] = srcArray[10];
            result[11] = 0.0f;
            result[12] = -(srcArray[0] * srcArray[12] + srcArray[1] * srcArray[13] + srcArray[2] * srcArray[14]);
            result[13] = -(srcArray[4] * srcArray[12] + srcArray[5] * srcArray[13] + srcArray[6] * srcArray[14]);
            result[14] = -(srcArray[8] * srcArray[12] + srcArray[9] * srcArray[13] + srcArray[10] * srcArray[14]);
            result[15] = 1.0f;

            return *(Matrix4x4*)result;
        }

        public unsafe static Matrix4x4 CreateProjectionFov(float tanAngleLeft,
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
